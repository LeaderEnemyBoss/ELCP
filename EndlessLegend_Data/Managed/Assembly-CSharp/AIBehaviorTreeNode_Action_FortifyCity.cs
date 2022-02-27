using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_FortifyCity : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.ticket = null;
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
			Army army;
			AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
			if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
			float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
			float costInActionPoints = this.armyActionFortify.GetCostInActionPoints();
			if (propertyValue <= propertyValue2 + costInActionPoints)
			{
				aiBehaviorTree.ErrorCode = 33;
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
			City city = aiBehaviorTree.Variables[this.TargetVarName] as City;
			if (city == null)
			{
				return State.Failure;
			}
			this.failuresFlags.Clear();
			if (!this.armyActionFortify.CanExecute(army, ref this.failuresFlags, new object[0]))
			{
				return State.Failure;
			}
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
			if (service2 != null)
			{
				IEnumerable<Encounter> enumerable = service2;
				if (enumerable != null)
				{
					bool flag = enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(army.GUID, false));
					if (flag)
					{
						return State.Running;
					}
				}
			}
			this.orderExecuted = false;
			this.armyActionFortify.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.ticket, new EventHandler<TicketRaisedEventArgs>(this.Order_TicketRaised), new object[]
			{
				city
			});
			return State.Running;
		}
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
		ArmyAction armyAction;
		if (database.TryGetValue("ArmyActionFortify", out armyAction))
		{
			this.armyActionFortify = (armyAction as ArmyAction_Fortify);
		}
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private ArmyAction_Fortify armyActionFortify;

	private List<StaticString> failuresFlags = new List<StaticString>();

	private bool orderExecuted;

	private Ticket ticket;
}
