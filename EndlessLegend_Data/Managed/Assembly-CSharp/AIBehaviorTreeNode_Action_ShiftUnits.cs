using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AIBehaviorTreeNode_Action_ShiftUnits : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.aiDataRepositoryHelper = null;
		this.accountManager = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.ticket != null)
		{
			if (!this.orderExecuted)
			{
				return State.Running;
			}
			if (this.ticket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderExecuted = false;
				this.ticket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.orderExecuted = false;
			this.ticket = null;
			return State.Success;
		}
		else
		{
			if (!this.isDlcActive)
			{
				return State.Success;
			}
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			foreach (Unit unit in army.Units)
			{
				if (unit.IsShifter() && !unit.IsInCurrentSeasonForm())
				{
					return State.Success;
				}
			}
			AIData_Army aidata_Army;
			if (!this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(army.GUID, out aidata_Army) || !aiBehaviorTree.AICommander.MayUseShift(aidata_Army.CommanderMission))
			{
				return State.Success;
			}
			if (!this.orbAIHelper.MaySwitchUnit(aiBehaviorTree.AICommander.Empire.Index))
			{
				return State.Success;
			}
			if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
			{
				aiBehaviorTree.LogError("${0} not set", new object[]
				{
					this.TargetVarName
				});
				return State.Failure;
			}
			IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
			if (!(gameEntity is IWorldPositionable))
			{
				return State.Failure;
			}
			if (!this.gameEntityRepositoryService.Contains(gameEntity.GUID))
			{
				return State.Failure;
			}
			Garrison garrison = gameEntity as Garrison;
			if (garrison.IsInEncounter)
			{
				return State.Running;
			}
			float expensePriority = this.ComputeShiftPriority(aidata_Army, garrison);
			float availableAmount = this.accountManager.GetAvailableAmount(AILayer_AccountManager.OrbAccountName, expensePriority);
			if (availableAmount <= 0f)
			{
				return State.Success;
			}
			GameEntityGUID[] shiftingUnitGuids = null;
			float expense = 0f;
			this.SelectUnitToShift(aidata_Army, availableAmount, out expense, out shiftingUnitGuids);
			if (!this.accountManager.TryMakeUnexpectedImmediateExpense(AILayer_AccountManager.OrbAccountName, expense, expensePriority))
			{
				return State.Success;
			}
			OrderForceShiftUnits order = new OrderForceShiftUnits(aiBehaviorTree.AICommander.Empire.Index, shiftingUnitGuids);
			this.orderExecuted = false;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised));
			return State.Running;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		this.isDlcActive = true;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (!service.IsShared(DownloadableContent13.ReadOnlyName))
		{
			this.isDlcActive = false;
		}
		if (!(aiBehaviorTree.AICommander.Empire is MajorEmpire))
		{
			this.isDlcActive = false;
		}
		if (this.isDlcActive)
		{
			IGameService service2 = Services.GetService<IGameService>();
			Diagnostics.Assert(service2 != null);
			this.gameEntityRepositoryService = service2.Game.Services.GetService<IGameEntityRepositoryService>();
			this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
			this.orbAIHelper = AIScheduler.Services.GetService<IOrbAIHelper>();
			this.intelligenceAiHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
			AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.accountManager = entity.GetLayer<AILayer_AccountManager>();
		}
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private void SelectUnitToShift(AIData_Army army, float maxCost, out float cost, out GameEntityGUID[] shiftingUnitGuids)
	{
		cost = 0f;
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		DepartmentOfTheTreasury agency = army.Army.Empire.GetAgency<DepartmentOfTheTreasury>();
		foreach (Unit unit in army.Army.Units)
		{
			if (this.aiDataRepositoryHelper.IsGUIDValid(unit.GUID))
			{
				if (unit.CanShift())
				{
					ConstructionCost unitForceShiftingCost = agency.GetUnitForceShiftingCost(unit);
					float value = unitForceShiftingCost.Value;
					if (cost + value > maxCost)
					{
						break;
					}
					cost += value;
					list.Add(unit.GUID);
				}
			}
		}
		shiftingUnitGuids = list.ToArray();
	}

	private float ComputeShiftPriority(AIData_Army army, Garrison target)
	{
		float num = 0f;
		float num2 = 0f;
		this.intelligenceAiHelper.EstimateMPInBattleground(army.Army, target, ref num2, ref num);
		float num3 = num / num2;
		if (num3 > 1.5f || num3 < 0.5f)
		{
			return 0f;
		}
		float propertyValue = target.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float a = num / propertyValue;
		float propertyValue2 = army.Army.Empire.GetPropertyValue(SimulationProperties.MilitaryPower);
		float b = num2 / propertyValue2;
		float num4 = Mathf.Max(a, b);
		num4 = Mathf.Clamp01(num4);
		float normalizedScore = 0.3f;
		return AILayer.Boost(normalizedScore, num4);
	}

	private bool isDlcActive;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AILayer_AccountManager accountManager;

	private IOrbAIHelper orbAIHelper;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IIntelligenceAIHelper intelligenceAiHelper;

	private bool orderExecuted;

	private Ticket ticket;
}
