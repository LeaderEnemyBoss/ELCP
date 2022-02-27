using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;

public class AIBehaviorTreeNode_Action_Colonize : AIBehaviorTreeNode_Action
{
	[XmlAttribute]
	public string PathVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		State result;
		Army army;
		if (this.orderPosted)
		{
			if (this.orderExecuted)
			{
				this.orderExecuted = false;
				this.orderPosted = false;
				result = State.Success;
			}
			else
			{
				result = State.Running;
			}
		}
		else if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			result = State.Failure;
		}
		else
		{
			bool flag = true;
			for (int i = 0; i < army.StandardUnits.Count; i++)
			{
				if (army.StandardUnits[i].CheckUnitAbility(UnitAbility.ReadonlyResettle, -1))
				{
					flag = false;
					break;
				}
			}
			ArmyAction armyAction = null;
			List<ArmyAction> list = new List<ArmyAction>();
			if (flag)
			{
				list = new List<ArmyAction>(new List<ArmyAction>(Databases.GetDatabase<ArmyAction>(false).GetValues()).FindAll((ArmyAction match) => match is ArmyAction_Colonization));
			}
			else
			{
				list = new List<ArmyAction>(new List<ArmyAction>(Databases.GetDatabase<ArmyAction>(false).GetValues()).FindAll((ArmyAction match) => match is ArmyAction_Resettle));
			}
			List<StaticString> list2 = new List<StaticString>();
			for (int j = 0; j < list.Count; j++)
			{
				if (list[j].CanExecute(army, ref list2, new object[0]))
				{
					armyAction = list[j];
				}
			}
			if (armyAction != null)
			{
				this.orderExecuted = false;
				this.orderPosted = true;
				if (flag)
				{
					this.CityLocation = army.WorldPosition;
					this.Empire = army.Empire;
					Ticket ticket;
					armyAction.Execute(army, aiBehaviorTree.AICommander.Empire.PlayerControllers.AI, out ticket, new EventHandler<TicketRaisedEventArgs>(this.ArmyAction_TicketRaised), new object[0]);
				}
				else
				{
					OrderResettle order = new OrderResettle(army.Empire.Index, army.GUID, armyAction.Name);
					Ticket ticket;
					aiBehaviorTree.AICommander.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderResettle_TicketRaised));
				}
				result = State.Running;
			}
			else
			{
				aiBehaviorTree.ErrorCode = 22;
				result = State.Failure;
			}
		}
		return result;
	}

	protected override bool Initialize(AIBehaviorTree aiBehaviorTree)
	{
		return base.Initialize(aiBehaviorTree);
	}

	private void ArmyAction_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.OrderCityBuilding();
		this.CityLocation = WorldPosition.Invalid;
		this.Empire = null;
		this.orderExecuted = true;
	}

	private void OrderCityBuilding()
	{
		DepartmentOfTheInterior agency = this.Empire.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfIndustry agency2 = this.Empire.GetAgency<DepartmentOfIndustry>();
		if (this.CityLocation.IsValid && agency != null)
		{
			City closestCityFromWorldPosition = agency.GetClosestCityFromWorldPosition(this.CityLocation, true);
			if ((closestCityFromWorldPosition != null || closestCityFromWorldPosition.Empire == this.Empire) && agency2.GetConstructionQueue(closestCityFromWorldPosition).PendingConstructions.Count <= 0)
			{
				List<string> list = new List<string>
				{
					"CityImprovementIndustry0",
					"CityImprovementDust0",
					"CityImprovementFood0",
					"CityImprovementApproval1",
					"CityImprovementScience0"
				};
				List<DepartmentOfIndustry.ConstructibleElement> list2 = new List<DepartmentOfIndustry.ConstructibleElement>();
				DepartmentOfIndustry.ConstructibleElement constructibleElement = null;
				foreach (DepartmentOfIndustry.ConstructibleElement constructibleElement2 in agency2.ConstructibleElementDatabase.GetAvailableConstructibleElements(new StaticString[]
				{
					CityImprovementDefinition.ReadOnlyCategory
				}))
				{
					if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(closestCityFromWorldPosition, constructibleElement2, new string[]
					{
						ConstructionFlags.Prerequisite
					}))
					{
						if (agency.Cities.Count == 1 && constructibleElement2.ToString().Contains("CityImprovementFIDS"))
						{
							constructibleElement = constructibleElement2;
							break;
						}
						if (list.Contains(constructibleElement2.ToString()))
						{
							list2.Add(constructibleElement2);
						}
					}
				}
				if (constructibleElement == null && list2.Count > 0)
				{
					using (List<string>.Enumerator enumerator = list.GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							string important = enumerator.Current;
							if (list2.Any((DepartmentOfIndustry.ConstructibleElement item) => item.Name == important))
							{
								constructibleElement = list2.First((DepartmentOfIndustry.ConstructibleElement item) => item.Name == important);
								break;
							}
						}
					}
				}
				if (constructibleElement != null)
				{
					OrderQueueConstruction order = new OrderQueueConstruction(this.Empire.Index, closestCityFromWorldPosition.GUID, constructibleElement, string.Empty);
					Ticket ticket;
					this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, null);
				}
			}
		}
	}

	private void OrderResettle_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.orderExecuted = true;
	}

	private bool orderExecuted;

	private bool orderPosted;

	private WorldPosition CityLocation;

	private Empire Empire;
}
