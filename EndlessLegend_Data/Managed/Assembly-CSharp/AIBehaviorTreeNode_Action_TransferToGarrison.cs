using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Action_TransferToGarrison : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string DestinationVarName { get; set; }

	public override void Release()
	{
		base.Release();
		this.currentTicket = null;
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
		else if (this.heroTicket != null)
		{
			if (!this.heroTicket.Raised)
			{
				return State.Running;
			}
			if (this.heroTicket.PostOrderResponse != PostOrderResponse.Processed)
			{
				aiBehaviorTree.ErrorCode = 36;
				return State.Failure;
			}
			this.heroTicket = null;
			return State.Running;
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
			WorldPosition worldPosition = (WorldPosition)aiBehaviorTree.Variables[this.DestinationVarName];
			if (!worldPosition.IsValid)
			{
				aiBehaviorTree.LogError("Destination is invalid.", new object[0]);
				aiBehaviorTree.ErrorCode = 2;
				return State.Failure;
			}
			List<GameEntityGUID> list = new List<GameEntityGUID>();
			foreach (Unit unit in army.StandardUnits)
			{
				list.Add(unit.GUID);
			}
			if (list.Count == 0)
			{
				return State.Success;
			}
			bool flag2 = army.Hero != null;
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			IPathfindingService service3 = service.Game.Services.GetService<IPathfindingService>();
			Diagnostics.Assert(service3 != null);
			City city = service2.GetRegion(worldPosition).City;
			if (city != null)
			{
				District district = service2.GetDistrict(worldPosition);
				if (district != null)
				{
					GameEntityGUID destinationGuid = GameEntityGUID.Zero;
					if (city.Camp != null && city.Camp.ContainsDistrict(district.GUID))
					{
						destinationGuid = city.Camp.GUID;
					}
					else if (District.IsACityTile(district))
					{
						destinationGuid = city.GUID;
					}
					if (destinationGuid.IsValid)
					{
						if (flag2)
						{
							if (!District.IsACityTile(district) || city.Hero != null)
							{
								this.UnassignHero(army);
								return State.Running;
							}
							list.Add(army.Hero.GUID);
						}
						OrderTransferUnits order = new OrderTransferUnits(army.Empire.Index, army.GUID, destinationGuid, list.ToArray(), false);
						aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out this.currentTicket, null);
						return State.Running;
					}
				}
			}
			Army armyAtPosition = service2.GetArmyAtPosition(worldPosition);
			if (armyAtPosition != null)
			{
				if (flag2)
				{
					if (armyAtPosition.Hero != null)
					{
						this.UnassignHero(army);
						return State.Running;
					}
					list.Add(army.Hero.GUID);
				}
				OrderTransferUnits order2 = new OrderTransferUnits(army.Empire.Index, army.GUID, armyAtPosition.GUID, list.ToArray(), false);
				aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order2, out this.currentTicket, null);
				return State.Running;
			}
			if (service3.IsTileStopable(worldPosition, army, (PathfindingFlags)0, null))
			{
				OrderTransferGarrisonToNewArmy order3 = new OrderTransferGarrisonToNewArmy(army.Empire.Index, army.GUID, list.ToArray(), worldPosition, StaticString.Empty, false, true, true);
				aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order3, out this.currentTicket, null);
				return State.Running;
			}
			aiBehaviorTree.LogError("No valid destination found.", new object[0]);
			aiBehaviorTree.ErrorCode = 2;
			return State.Failure;
		}
	}

	private void UnassignHero(Army army)
	{
		if (army.Hero != null)
		{
			OrderChangeHeroAssignment orderChangeHeroAssignment = new OrderChangeHeroAssignment(army.Empire.Index, army.Hero.GUID, GameEntityGUID.Zero);
			orderChangeHeroAssignment.IgnoreCooldown = true;
			army.Empire.PlayerControllers.AI.PostOrder(orderChangeHeroAssignment, out this.heroTicket, null);
		}
	}

	private Ticket currentTicket;

	private Ticket heroTicket;
}
