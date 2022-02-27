using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;

public class QuestBehaviourTreeNode_Action_Elimination : QuestBehaviourTreeNode_Action
{
	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Diagnostics.Assert(service.Game != null);
		global::Empire initiator = questBehaviour.Initiator;
		Diagnostics.Assert(initiator.SimulationObject != null);
		if (!initiator.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated))
		{
			DepartmentOfDefense agency = initiator.GetAgency<DepartmentOfDefense>();
			if (agency != null)
			{
				IEncounterRepositoryService service2 = service.Game.Services.GetService<IEncounterRepositoryService>();
				if (service2 != null)
				{
					IEnumerable<Encounter> enumerable = service2;
					if (enumerable != null)
					{
						List<Encounter> list = new List<Encounter>();
						foreach (Encounter encounter in enumerable)
						{
							if (encounter != null)
							{
								for (int i = 0; i < encounter.Empires.Count; i++)
								{
									if (encounter.Empires[i].Index == initiator.Index && encounter.EncounterState != EncounterState.BattleHasEnded)
									{
										list.Add(encounter);
									}
								}
							}
						}
						global::Empire.PlayerControllersContainer playerControllers = (initiator as MajorEmpire).PlayerControllers;
						if (playerControllers != null && playerControllers.Server != null)
						{
							for (int j = 0; j < list.Count; j++)
							{
								Encounter encounter2 = list[j];
								OrderEndEncounter order = new OrderEndEncounter(encounter2.GUID, true);
								playerControllers.Server.PostOrder(order);
								OrderDestroyEncounter order2 = new OrderDestroyEncounter(encounter2.GUID);
								playerControllers.Server.PostOrder(order2);
							}
						}
					}
				}
			}
			SimulationDescriptor simulationDescriptor = null;
			IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
			if (database != null)
			{
				database.TryGetValue("EmpireEliminated", out simulationDescriptor);
			}
			if (simulationDescriptor != null)
			{
				initiator.AddDescriptor(simulationDescriptor, false);
				initiator.Refresh(true);
			}
			else
			{
				initiator.SimulationObject.Tags.AddTag(global::Empire.TagEmpireEliminated);
			}
		}
		ISessionService service3 = Services.GetService<ISessionService>();
		Diagnostics.Assert(service3 != null && service3.Session != null);
		if (service3.Session.IsHosting)
		{
			service3.Session.SetLobbyData(string.Format("Empire{0}Eliminated", initiator.Index), true, true);
		}
		IPlayerControllerRepositoryService service4 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		IPlayerControllerRepositoryControl playerControllerRepositoryControl = service4 as IPlayerControllerRepositoryControl;
		if (playerControllerRepositoryControl != null)
		{
			global::PlayerController playerControllerById = playerControllerRepositoryControl.GetPlayerControllerById("server");
			if (playerControllerById != null)
			{
				if (initiator is MajorEmpire)
				{
					MajorEmpire majorEmpire = initiator as MajorEmpire;
					if (majorEmpire.TamedKaijus.Count > 0)
					{
						majorEmpire.ServerUntameAllKaijus();
					}
				}
				OrderEliminateEmpire order3 = new OrderEliminateEmpire(questBehaviour.Initiator.Index);
				playerControllerById.PostOrder(order3);
			}
		}
		return State.Success;
	}
}
