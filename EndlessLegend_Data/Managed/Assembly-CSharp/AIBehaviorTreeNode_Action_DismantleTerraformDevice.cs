using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_DismantleTerraformDevice : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_DismantleTerraformDevice()
	{
		this.TargetVarName = string.Empty;
	}

	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.orderTicket = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.orderTicket != null)
		{
			if (!this.orderTicket.Raised)
			{
				return State.Running;
			}
			if (this.orderTicket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed)
			{
				this.orderTicket = null;
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			this.orderTicket = null;
			return State.Success;
		}
		else
		{
			Army army;
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			if (army.IsDismantlingDevice)
			{
				aiBehaviorTree.ErrorCode = 37;
				return State.Failure;
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
			if (!(gameEntity is IWorldPositionable) || !(gameEntity is TerraformDevice))
			{
				aiBehaviorTree.ErrorCode = 10;
				return State.Failure;
			}
			if (!service2.Contains(gameEntity.GUID))
			{
				return State.Success;
			}
			if ((gameEntity as TerraformDevice).TurnsToActivate() < 1999)
			{
				return State.Success;
			}
			if ((gameEntity as TerraformDevice).DismantlingArmy != null)
			{
				aiBehaviorTree.ErrorCode = 37;
				return State.Failure;
			}
			Diagnostics.Assert(AIScheduler.Services != null);
			if (service.Game.Services.GetService<IWorldPositionningService>().GetDistance(army.WorldPosition, (gameEntity as IWorldPositionable).WorldPosition) != 1)
			{
				aiBehaviorTree.ErrorCode = 12;
				return State.Failure;
			}
			IEncounterRepositoryService service3 = service.Game.Services.GetService<IEncounterRepositoryService>();
			if (service3 != null)
			{
				IEnumerable<Encounter> enumerable = service3;
				if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false)))
				{
					return State.Running;
				}
			}
			OrderToggleDismantleDevice order = new OrderToggleDismantleDevice(army.Empire.Index, army.GUID, gameEntity.GUID, true);
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.orderTicket, null);
			return State.Running;
		}
	}

	private Ticket orderTicket;
}
