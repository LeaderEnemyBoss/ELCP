using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_SearchInRuin : AIBehaviorTreeNode_Action
{
	public AIBehaviorTreeNode_Action_SearchInRuin()
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
			bool flag = this.orderTicket.PostOrderResponse != PostOrderResponse.Processed;
			if (flag)
			{
				OrderInteractWith orderInteractWith = this.orderTicket.Order as OrderInteractWith;
				IGameEntity gameEntity = null;
				if (service2.TryGetValue(orderInteractWith.TargetGUID, out gameEntity))
				{
					PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
					if (pointOfInterest != null)
					{
						pointOfInterest.Interaction.Bits |= 1 << orderInteractWith.EmpireIndex;
					}
				}
				aiBehaviorTree.ErrorCode = 30;
				this.orderTicket = null;
				return State.Failure;
			}
			this.orderTicket = null;
			return State.Success;
		}
		else
		{
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
			{
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
			IGameEntity gameEntity2 = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
			if (!(gameEntity2 is IWorldPositionable) || !(gameEntity2 is PointOfInterest))
			{
				aiBehaviorTree.ErrorCode = 10;
				return State.Failure;
			}
			Diagnostics.Assert(AIScheduler.Services != null);
			IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
			if (service3.GetDistance(army.WorldPosition, (gameEntity2 as IWorldPositionable).WorldPosition) != 1)
			{
				aiBehaviorTree.ErrorCode = 12;
				return State.Failure;
			}
			IEncounterRepositoryService service4 = service.Game.Services.GetService<IEncounterRepositoryService>();
			if (service4 != null)
			{
				IEnumerable<Encounter> enumerable = service4;
				if (enumerable != null)
				{
					bool flag2 = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false));
					if (flag2)
					{
						return State.Running;
					}
				}
			}
			OrderInteractWith orderInteractWith2 = new OrderInteractWith(army.Empire.Index, army.GUID, "ArmyActionSearch");
			orderInteractWith2.WorldPosition = army.WorldPosition;
			orderInteractWith2.Tags.AddTag("Interact");
			orderInteractWith2.TargetGUID = gameEntity2.GUID;
			aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(orderInteractWith2, out this.orderTicket, null);
			return State.Running;
		}
	}

	private Ticket orderTicket;
}
