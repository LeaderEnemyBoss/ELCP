using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_TeleportToCity : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string DestinationVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.currentTicket = null;
		this.worldPositionningService = null;
		this.encounterRepositoryService = null;
	}

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (this.currentTicket != null)
		{
			if (!this.currentTicket.Raised)
			{
				return State.Running;
			}
			bool flag = this.currentTicket.PostOrderResponse == PostOrderResponse.PreprocessHasFailed || this.currentTicket.PostOrderResponse == PostOrderResponse.AuthenticationHasFailed;
			this.currentTicket = null;
			if (flag)
			{
				aiBehaviorTree.ErrorCode = 1;
				return State.Failure;
			}
			return State.Success;
		}
		else
		{
			if (!aiBehaviorTree.Variables.ContainsKey(this.DestinationVarName))
			{
				aiBehaviorTree.LogError("{0} not set", new object[]
				{
					this.DestinationVarName
				});
				return State.Failure;
			}
			City city = (City)aiBehaviorTree.Variables[this.DestinationVarName];
			ArmyAction armyAction = null;
			if (city != null)
			{
				if (!ELCPUtilities.CanTeleportToCity(city, army, this.worldPositionningService.GetRegion(army.WorldPosition), this.worldPositionningService, this.encounterRepositoryService))
				{
					return State.Failure;
				}
				List<ArmyAction> list = new List<ArmyAction>(new List<ArmyAction>(Databases.GetDatabase<ArmyAction>(false).GetValues()).FindAll((ArmyAction match) => match is ArmyAction_Teleport));
				List<StaticString> list2 = new List<StaticString>();
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].CanExecute(army, ref list2, new object[0]))
					{
						armyAction = list[i];
						break;
					}
				}
				if (armyAction != null)
				{
					armyAction.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out this.currentTicket, null, new object[]
					{
						city
					});
					return State.Running;
				}
			}
			Diagnostics.LogError("ELCP: AIBehaviorTreeNode_Action_TeleportToCity failed for {0}/{1} {5} -> {6}, {2} {3} {4}", new object[]
			{
				army.LocalizedName,
				army.Empire,
				army.GetPropertyBaseValue(SimulationProperties.Movement),
				city != null,
				armyAction != null,
				army.WorldPosition,
				(city != null) ? city.WorldPosition.ToString() : "null"
			});
			return State.Failure;
		}
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.encounterRepositoryService = service.Game.Services.GetService<IEncounterRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	private Ticket currentTicket;

	private IWorldPositionningService worldPositionningService;

	private IEncounterRepositoryService encounterRepositoryService;
}
