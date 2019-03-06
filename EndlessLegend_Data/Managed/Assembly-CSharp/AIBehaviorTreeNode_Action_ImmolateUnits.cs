using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_ImmolateUnits : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_ImmolateUnits()
	{
		this.PowerRatioCeil = float.MaxValue;
		this.PowerRatioFloor = float.MinValue;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }

	[XmlAttribute]
	public float PowerRatioCeil { get; set; }

	[XmlAttribute]
	public float PowerRatioFloor { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.orderTicket != null)
		{
			if (!this.orderExecuted)
			{
				return State.Running;
			}
			if (this.orderTicket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderExecuted = false;
				this.orderTicket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.orderExecuted = false;
			this.orderTicket = null;
			return State.Success;
		}
		else
		{
			if (!Services.GetService<IDownloadableContentService>().IsShared(DownloadableContent19.ReadOnlyName))
			{
				return State.Success;
			}
			if (!Services.GetService<IGameService>().Game.Services.GetService<ISeasonService>().GetCurrentSeason().SeasonDefinition.SeasonType.Equals(Season.ReadOnlyHeatWave))
			{
				return State.Success;
			}
			Army army = null;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			Garrison garrison = aiBehaviorTree.Variables[this.TargetVarName] as Garrison;
			if (garrison == null)
			{
				return State.Success;
			}
			if (garrison.IsInEncounter)
			{
				return State.Running;
			}
			if (!this.EvaluateImmolationNeed(army, garrison))
			{
				return State.Success;
			}
			GameEntityGUID[] array = null;
			this.SelectImmolableUnits(army, out array);
			if (array.Length < 1)
			{
				return State.Success;
			}
			OrderImmolateUnits order = new OrderImmolateUnits(army.Empire.Index, array);
			this.orderExecuted = false;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised));
			return State.Running;
		}
	}

	private bool EvaluateImmolationNeed(Army army, Garrison target)
	{
		float num = 0f;
		float num2 = 0f;
		AIScheduler.Services.GetService<IIntelligenceAIHelper>().EstimateMPInBattleground(army, target, ref num2, ref num);
		float num3 = num / num2;
		return num3 < this.PowerRatioCeil && num3 > this.PowerRatioFloor;
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private void SelectImmolableUnits(Army sourceArmy, out GameEntityGUID[] immolableUnitsGUIDs)
	{
		List<GameEntityGUID> list = new List<GameEntityGUID>();
		foreach (Unit unit in sourceArmy.Units)
		{
			if (unit.IsImmolableUnit() && !unit.IsAlreadyImmolated())
			{
				list.Add(unit.GUID);
			}
		}
		immolableUnitsGUIDs = list.ToArray();
	}

	private bool orderExecuted;

	private Ticket orderTicket;
}
