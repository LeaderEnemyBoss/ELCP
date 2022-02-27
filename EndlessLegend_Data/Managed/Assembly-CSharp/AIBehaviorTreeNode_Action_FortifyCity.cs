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
	public AIBehaviorTreeNode_Action_FortifyCity()
	{
		this.failuresFlags = new List<StaticString>();
		this.TargetVarName = string.Empty;
	}

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
			if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) != AIArmyMission.AIArmyMissionErrorCode.None)
			{
				return State.Failure;
			}
			if (this.worldPositionningService.IsWaterTile(army.WorldPosition))
			{
				return State.Failure;
			}
			float propertyValue = army.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
			float propertyValue2 = army.GetPropertyValue(SimulationProperties.ActionPointsSpent);
			float costInActionPoints = this.armyActionFortify.GetCostInActionPoints();
			if (propertyValue < propertyValue2 + costInActionPoints)
			{
				aiBehaviorTree.ErrorCode = 33;
				return State.Failure;
			}
			City city = null;
			if (this.TargetVarName == string.Empty)
			{
				District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
				if (district == null || !District.IsACityTile(district) || (district.City.Empire.Index != army.Empire.Index && !army.Empire.GetAgency<DepartmentOfForeignAffairs>().IsFriend(district.City.Empire)))
				{
					return State.Failure;
				}
				city = district.City;
			}
			else
			{
				if (!aiBehaviorTree.Variables.ContainsKey(this.TargetVarName))
				{
					aiBehaviorTree.LogError("${0} not set", new object[]
					{
						this.TargetVarName
					});
					return State.Failure;
				}
				city = (aiBehaviorTree.Variables[this.TargetVarName] as City);
				if (city == null)
				{
					return State.Failure;
				}
			}
			if (city.GetPropertyValue(SimulationProperties.CityDefensePoint) < 50f)
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
				if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(city.GUID, false)))
				{
					return State.Running;
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
		ArmyAction armyAction;
		if (Databases.GetDatabase<ArmyAction>(false).TryGetValue("ArmyActionFortify", out armyAction))
		{
			this.armyActionFortify = (armyAction as ArmyAction_Fortify);
		}
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		return base.Initialize(aiBehaviorTree);
	}

	private void Order_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private ArmyAction_Fortify armyActionFortify;

	private List<StaticString> failuresFlags;

	private bool orderExecuted;

	private Ticket ticket;

	private IWorldPositionningService worldPositionningService;
}
