using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Input;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

[OrderProcessor(typeof(OrderGoToAndAttack), "GoToAndAttack")]
[OrderProcessor(typeof(OrderCancelMove), "CancelMove")]
[OrderProcessor(typeof(OrderTeleportArmyToCity), "TeleportArmyToCity")]
[OrderProcessor(typeof(OrderGoTo), "GoTo")]
[OrderProcessor(typeof(OrderGoToAndMerge), "GoToAndMerge")]
[OrderProcessor(typeof(OrderTeleportArmy), "TeleportArmy")]
[OrderProcessor(typeof(OrderGoToAndResettle), "OrderGoToAndResettle")]
[OrderProcessor(typeof(OrderGoToAndTerraform), "OrderGoToAndTerraform")]
[OrderProcessor(typeof(OrderGoToAndColonize), "OrderGoToAndColonize")]
[OrderProcessor(typeof(OrderGoToAndExecute), "GoToAndExecute")]
[OrderProcessor(typeof(OrderMoveTo), "MoveTo")]
[OrderProcessor(typeof(OrderContinueGoToInstruction), "ContinueGoToInstruction")]
[OrderProcessor(typeof(OrderResetGoToInstruction), "ResetGoToInstruction")]
public class DepartmentOfTransportation : Agency, IXmlSerializable, IGameStateUpdatable<GameServerState_Turn_Main>, IGameStateUpdatable<GameServerState_Turn_Finished>
{
	public DepartmentOfTransportation(global::Empire empire) : base(empire)
	{
	}

	public event EventHandler<ArmyMoveEndedEventArgs> ArmyPositionChange;

	public event EventHandler<ArmyTeleportedToCityEventArgs> ArmyTeleportedToCity;

	void IGameStateUpdatable<GameServerState_Turn_Main>.Update(GameInterface gameInterface)
	{
		this.UpdatePendingGoToInstructions(gameInterface);
		if (this.keyMappingService.GetKeyDown(KeyAction.ControlBindingsContinueArmiesInstructions))
		{
			this.ContinueGoToInstruction();
		}
	}

	void IGameStateUpdatable<GameServerState_Turn_Finished>.Update(GameInterface gameInterface)
	{
		this.UpdatePendingGoToInstructions(gameInterface);
	}

	public List<ArmyGoToInstruction> ArmiesWithPendingGoToInstructions
	{
		get
		{
			return this.armiesWithPendingGoToInstructions;
		}
	}

	private void UpdatePendingGoToInstructions(GameInterface gameInterface)
	{
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		if (this.armiesWithPendingGoToInstructions.Count <= 0)
		{
			return;
		}
		for (int i = this.armiesWithPendingGoToInstructions.Count - 1; i >= 0; i--)
		{
			ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions[i];
			Diagnostics.Assert(armyGoToInstruction != null);
			if (!armyGoToInstruction.IsMoveFinished && !armyGoToInstruction.IsMoveCancelled)
			{
				armyGoToInstruction.Tick(gameInterface);
			}
			if (armyGoToInstruction.IsMoveFinished)
			{
				this.armiesWithPendingGoToInstructions.RemoveAt(i);
			}
		}
	}

	private IEnumerator GameServerState_Turn_Finished_ContinueGoToInstruction(string context, string name)
	{
		global::Empire empire = base.Empire as global::Empire;
		Diagnostics.Assert(empire != null);
		List<Ticket> tickets = new List<Ticket>();
		bool isSomethingMoving = true;
		while (this.armiesWithPendingGoToInstructions.Count > 0 && isSomethingMoving)
		{
			tickets.Clear();
			isSomethingMoving = false;
			Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
			for (int index = this.armiesWithPendingGoToInstructions.Count - 1; index >= 0; index--)
			{
				ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions[index];
				Diagnostics.Assert(armyGoToInstruction != null);
				if (!armyGoToInstruction.IsFinished)
				{
					Army army = this.DepartmentOfDefense.GetArmy(armyGoToInstruction.ArmyGUID);
					if (army != null)
					{
						isSomethingMoving = true;
						if (army.IsAbleToMove)
						{
							if (!armyGoToInstruction.IsMoving)
							{
								if (!armyGoToInstruction.IsMoveCancelled)
								{
									global::Order order = new OrderContinueGoToInstruction(base.Empire.Index, armyGoToInstruction);
									Diagnostics.Assert(empire.PlayerControllers.Server != null, "Empire player controller (server) is null.");
									Ticket ticket;
									empire.PlayerControllers.Server.PostOrder(order, out ticket, null);
									tickets.Add(ticket);
								}
							}
						}
					}
				}
			}
			bool allOrderPreprocessed = false;
			while (!allOrderPreprocessed)
			{
				allOrderPreprocessed = true;
				for (int index2 = 0; index2 < tickets.Count; index2++)
				{
					Ticket ticket2 = tickets[index2];
					if (ticket2 != null)
					{
						allOrderPreprocessed &= (ticket2.PostOrderResponse != PostOrderResponse.Undefined);
					}
				}
				yield return null;
			}
			yield return null;
		}
		yield break;
	}

	private void ContinueGoToInstruction()
	{
		IEncounterRepositoryService service = this.GameService.Game.Services.GetService<IEncounterRepositoryService>();
		if (service == null)
		{
			Diagnostics.LogError("encounterRepositoryService is null");
			return;
		}
		IEnumerable<Encounter> source = service;
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		for (int i = this.armiesWithPendingGoToInstructions.Count - 1; i >= 0; i--)
		{
			ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions[i];
			Diagnostics.Assert(armyGoToInstruction != null);
			if (!armyGoToInstruction.IsFinished)
			{
				Army army = this.DepartmentOfDefense.GetArmy(armyGoToInstruction.ArmyGUID);
				if (army != null)
				{
					if (army.IsAbleToMove && !army.IsMoving)
					{
						if (army.GetPropertyValue(SimulationProperties.Movement) > 0f)
						{
							if (army.WorldPath != null && !(army.WorldPosition == army.WorldPath.Destination))
							{
								if (!source.Any((Encounter encounter) => encounter != null && encounter.Contenders != null && encounter.Contenders.Any((Contender contender) => contender != null && contender.GUID == army.GUID)))
								{
									OrderContinueGoToInstruction order = new OrderContinueGoToInstruction(army.Empire.Index, army.GUID);
									army.Empire.PlayerControllers.Client.PostOrder(order);
								}
							}
						}
					}
				}
			}
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		reader.ReadStartElement("Server");
		ISessionService service = Services.GetService<ISessionService>();
		if (service.Session.IsHosting)
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("PendingArmyGoToInstructions");
			for (int i = 0; i < attribute; i++)
			{
				string attribute2 = reader.GetAttribute("AssemblyQualifiedName");
				Type type = Type.GetType(attribute2);
				ArmyGoToInstruction armyGoToInstruction = Activator.CreateInstance(type, true) as ArmyGoToInstruction;
				if (armyGoToInstruction != null)
				{
					reader.ReadElementSerializable<ArmyGoToInstruction>(ref armyGoToInstruction);
					if (armyGoToInstruction.ArmyGUID.IsValid)
					{
						this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
					}
				}
			}
			reader.ReadEndElement();
		}
		else
		{
			reader.Reader.Skip();
		}
		reader.ReadEndElement();
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("Server");
		writer.WriteStartElement("PendingArmyGoToInstructions");
		writer.WriteAttributeString<int>("Count", this.armiesWithPendingGoToInstructions.Count);
		for (int i = 0; i < this.armiesWithPendingGoToInstructions.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.armiesWithPendingGoToInstructions[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
		writer.WriteEndElement();
	}

	protected bool CancelMovePreprocessor(OrderCancelMove order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Preprocessor failed because game entity guid is invalid.");
			return false;
		}
		if (this.DepartmentOfDefense.GetArmy(order.GameEntityGUID) == null)
		{
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.GameEntityGUID);
		if (armyGoToInstruction != null)
		{
			if (!armyGoToInstruction.IsMoveCancelled)
			{
				armyGoToInstruction.Cancel(true);
			}
			this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
		}
		return true;
	}

	protected IEnumerator CancelMoveProcessor(OrderCancelMove order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			yield break;
		}
		army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
		army.Refresh(false);
		yield break;
	}

	protected bool ContinueGoToInstructionPreprocessor(OrderContinueGoToInstruction order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the game entity guid.");
			return false;
		}
		if (this.GameService == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the game service is null.");
			return false;
		}
		IEncounterRepositoryService service = this.GameService.Game.Services.GetService<IEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<Encounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((Encounter encounter) => encounter.EncounterState != EncounterState.BattleHasEnded && encounter.Contenders != null && encounter.Contenders.Exists((Contender contender) => contender.Garrison.GUID == order.GameEntityGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.DepartmentOfDefense != null);
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is null.");
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.GameEntityGUID);
		if (armyGoToInstruction == null)
		{
			return false;
		}
		if (armyGoToInstruction.IsMoving)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is already moving.");
			return false;
		}
		if (!army.IsAbleToMove)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move.");
			return false;
		}
		if (army.IsLocked)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move.");
			return false;
		}
		WorldPosition[] remainingPath = armyGoToInstruction.GetRemainingPath();
		if (remainingPath.Length < 2)
		{
			armyGoToInstruction.Cancel(false);
		}
		else
		{
			PathfindingResult pathfindingResult = new PathfindingResult(remainingPath, army, (PathfindingFlags)0);
			order.WorldPath = new WorldPath();
			order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
			if (order.WorldPath.IsValid)
			{
				armyGoToInstruction.Reset(order.WorldPath);
				armyGoToInstruction.Resume();
			}
			else
			{
				armyGoToInstruction.Cancel(false);
			}
			order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
		}
		return true;
	}

	protected IEnumerator ContinueGoToInstructionProcessor(OrderContinueGoToInstruction order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		army.SetWorldPathWithEstimatedTimeOfArrival(order.WorldPath, order.EstimatedTimeOfArrival);
		yield break;
	}

	protected bool GoToPreprocessor(OrderGoTo order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the game entity guid.");
			return false;
		}
		if (this.GameService == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the game service is null.");
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.GameEntityGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.DepartmentOfDefense != null);
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.GameEntityGUID);
		if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
		{
			if (armyGoToInstruction != null)
			{
				armyGoToInstruction.Cancel(false);
				return true;
			}
			return false;
		}
		else
		{
			if (armyGoToInstruction != null)
			{
				armyGoToInstruction.Cancel(true);
				this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
				armyGoToInstruction = null;
			}
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.Empire == army.Empire && region.City.BesiegingEmpire != null)
			{
				bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == army.WorldPosition);
				bool flag3 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == order.Destination);
				if (flag2)
				{
					if (!flag3)
					{
						Diagnostics.LogWarning("Order preprocessor failed because the army is in a besieged city and try to move out without breaking the siege first. Army = {0}", new object[]
						{
							army.GUID
						});
						return false;
					}
				}
				else if (flag3)
				{
					Diagnostics.LogWarning("Order preprocessor failed because the army is not in a besieged city and try to move in without breaking the siege first. Army = {0}", new object[]
					{
						army.GUID
					});
					return false;
				}
			}
			if (!army.IsAbleToMove)
			{
				Diagnostics.LogWarning("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
				{
					army.GUID
				});
				return false;
			}
			if (army.IsLocked)
			{
				Diagnostics.LogWarning("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
				{
					army.GUID
				});
				return false;
			}
			Diagnostics.Assert(this.PathfindingService != null);
			PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, order.Flags, null);
			if (pathfindingResult == null)
			{
				Diagnostics.LogWarning("Order preprocessor failed because it is impossible to find a path to reach destination {0} from {1}.", new object[]
				{
					order.WorldPath.Destination,
					army.WorldPosition
				});
				return false;
			}
			order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
			if (order.WorldPath.IsValid)
			{
				if (armyGoToInstruction == null)
				{
					armyGoToInstruction = new ArmyGoToInstruction(army.GUID);
					this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
				}
				armyGoToInstruction.Reset(order.WorldPath);
				armyGoToInstruction.Resume();
			}
			else if (armyGoToInstruction != null)
			{
				armyGoToInstruction.Cancel(false);
			}
			order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
			return true;
		}
	}

	protected IEnumerator GoToProcessor(OrderGoTo order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		army.SetWorldPathWithEstimatedTimeOfArrival(order.WorldPath, order.EstimatedTimeOfArrival);
		yield break;
	}

	private bool GoToAndAttackPreprocessor(OrderGoToAndAttack order)
	{
		Diagnostics.Log("GotoAndAttack Preprocessor. Attacker={0}, Defender={1}.", new object[]
		{
			order.GameEntityGUID,
			order.DefenderGUID
		});
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the attacker guid is not valid.");
			return false;
		}
		if (!order.DefenderGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the defender guid is not valid.");
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.GameEntityGUID) || encounter.IsGarrisonInEncounter(order.DefenderGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.DefenderGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender is not referenced.");
			return false;
		}
		if (!(gameEntity is IGameEntityWithWorldPosition))
		{
			Diagnostics.LogError("Order preprocessing failed because the defender is not a IWorldPositionable.");
			return false;
		}
		IGameEntityWithWorldPosition defender = gameEntity as IGameEntityWithWorldPosition;
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because it's impossible to retrieve the attacker army {0}.", new object[]
			{
				order.GameEntityGUID
			});
			return false;
		}
		if (defender is Army)
		{
			Army defenderArmy = defender as Army;
			if (defenderArmy.IsNaval != army.IsNaval)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the defender is a ship. Attacker={0}, Defender={1}.", new object[]
				{
					order.GameEntityGUID,
					order.DefenderGUID
				});
				return false;
			}
			DepartmentOfTransportation agency = defenderArmy.Empire.GetAgency<DepartmentOfTransportation>();
			if (agency != null)
			{
				ArmyGoToInstruction armyGoToInstruction = agency.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == defenderArmy.GUID);
				if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the defender is moving. Attacker={0}, Defender={1}.", new object[]
					{
						order.GameEntityGUID,
						order.DefenderGUID
					});
					return false;
				}
			}
		}
		if (base.Empire is MajorEmpire && !army.IsPrivateers)
		{
			DepartmentOfForeignAffairs agency2 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency2 != null);
			if (!agency2.CanAttack(gameEntity))
			{
				Diagnostics.LogWarning("Order preprocessing failed because the diplomatic relation doesn't authorize the attack.");
				return false;
			}
		}
		if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
		{
			return false;
		}
		Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
		if (region != null && region.City != null && region.City.Empire == army.Empire && region.City.BesiegingEmpire != null)
		{
			bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == army.WorldPosition);
			if (flag2 && !region.City.Districts.Any((District match) => match.Type == DistrictType.Exploitation && match.WorldPosition == defender.WorldPosition))
			{
				Diagnostics.LogWarning("Order preprocessor failed because the army is in a besieged city and try to attack an army which is not around the city.");
				return false;
			}
		}
		PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
		if (pathfindingResult != null)
		{
			order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
			ArmyGoToInstruction armyGoToInstruction2 = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
			if (order.WorldPath.IsValid)
			{
				if (armyGoToInstruction2 != null && !(armyGoToInstruction2 is ArmyGoToAndAttackInstruction))
				{
					armyGoToInstruction2.Cancel(true);
					this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction2);
					armyGoToInstruction2 = null;
				}
				if (armyGoToInstruction2 == null)
				{
					armyGoToInstruction2 = new ArmyGoToAndAttackInstruction(army.GUID, defender.GUID);
					this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction2);
				}
				armyGoToInstruction2.Reset(order.WorldPath);
				armyGoToInstruction2.Resume();
			}
			else if (armyGoToInstruction2 != null)
			{
				armyGoToInstruction2.Cancel(false);
			}
			order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
			return true;
		}
		return true;
	}

	private IEnumerator GoToAndAttackProcessor(OrderGoToAndAttack order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	private bool OrderGoToAndColonizePreprocessor(OrderGoToAndColonize order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army != null)
		{
			if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
			{
				return false;
			}
			bool flag = false;
			foreach (Unit unit in army.Units)
			{
				if (unit.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the army has not the ability to colonize.");
				return false;
			}
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.Empire == army.Empire && region.City.BesiegingEmpire != null)
			{
				bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == army.WorldPosition);
				if (flag2)
				{
					Diagnostics.LogWarning("Order preprocessor failed because the army is in a besieged city.");
					return false;
				}
			}
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			ArmyAction value = database.GetValue(order.ArmyActionName);
			if (value == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the army action '{0}' is invalid.", new object[]
				{
					order.ArmyActionName
				});
				return false;
			}
			if (!this.DepartmentOfTheInterior.CanColonizeRegion(order.Destination, value, true))
			{
				return false;
			}
			PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
			if (pathfindingResult != null)
			{
				order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
				ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
				if (order.WorldPath.IsValid)
				{
					if (armyGoToInstruction != null && !(armyGoToInstruction is ArmyGoToAndColonizeInstruction))
					{
						armyGoToInstruction.Cancel(true);
						this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						armyGoToInstruction = null;
					}
					if (armyGoToInstruction == null)
					{
						armyGoToInstruction = new ArmyGoToAndColonizeInstruction(army, order.ArmyActionName);
						this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
					}
					armyGoToInstruction.Reset(order.WorldPath);
					armyGoToInstruction.Resume();
				}
				else if (armyGoToInstruction != null)
				{
					armyGoToInstruction.Cancel(false);
				}
				order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
				return true;
			}
		}
		return true;
	}

	private IEnumerator OrderGoToAndColonizeProcessor(OrderGoToAndColonize order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	private bool GoToAndExecutePreprocessor(OrderGoToAndExecute order)
	{
		Diagnostics.Log("GoToAndExecutePreprocessor");
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army != null)
		{
			if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
			{
				return false;
			}
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			ArmyAction value = database.GetValue(order.ArmyActionName);
			if (value != null)
			{
				List<StaticString> list = new List<StaticString>();
				if (order.TargetGUID.IsValid)
				{
					IGameEntity gameEntity = this.GameEntityRepositoryService[order.TargetGUID];
					if (!value.CanExecute(army, ref list, new object[]
					{
						gameEntity
					}))
					{
						Diagnostics.LogWarning("Cannot execute the action '{0}' on target guid '{1}'", new object[]
						{
							order.ArmyActionName,
							order.TargetGUID
						});
						return false;
					}
				}
				else if (!value.CanExecute(army, ref list, new object[]
				{
					order.Destination
				}))
				{
					Diagnostics.LogWarning("Cannot execute the action '{0}' on target position '{1}'", new object[]
					{
						order.ArmyActionName,
						order.WorldPath.Destination
					});
					return false;
				}
			}
			PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
			if (pathfindingResult != null)
			{
				order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
				ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
				if (order.WorldPath.IsValid)
				{
					if (armyGoToInstruction != null && !(armyGoToInstruction is ArmyGoToAndExecuteInstruction))
					{
						armyGoToInstruction.Cancel(true);
						this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						armyGoToInstruction = null;
					}
					if (armyGoToInstruction == null)
					{
						armyGoToInstruction = new ArmyGoToAndExecuteInstruction(army.GUID, order.ArmyActionName, order.TargetGUID);
						this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
					}
					armyGoToInstruction.Reset(order.WorldPath);
					armyGoToInstruction.Resume();
				}
				else if (armyGoToInstruction != null)
				{
					armyGoToInstruction.Cancel(false);
				}
				order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
				return true;
			}
		}
		return true;
	}

	private IEnumerator GoToAndExecuteProcessor(OrderGoToAndExecute order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	private bool GoToAndMergePreprocessor(OrderGoToAndMerge order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the departure army guid is not valid.");
			return false;
		}
		if (!order.DestinationGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the destination guid is not valid.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.DestinationGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the destination is not referenced.");
			return false;
		}
		if (!(gameEntity is IGameEntityWithWorldPosition))
		{
			Diagnostics.LogError("Order preprocessing failed because the destination is not a IWorldPositionable.");
			return false;
		}
		IGameEntityWithWorldPosition gameEntityWithWorldPosition = gameEntity as IGameEntityWithWorldPosition;
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessing failed because it's impossible to retrieve the starting army {0}.", new object[]
			{
				order.GameEntityGUID
			});
			return false;
		}
		if (gameEntityWithWorldPosition is Army)
		{
			Army destinationArmy = gameEntityWithWorldPosition as Army;
			DepartmentOfTransportation agency = destinationArmy.Empire.GetAgency<DepartmentOfTransportation>();
			if (agency != null)
			{
				ArmyGoToInstruction armyGoToInstruction = agency.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == destinationArmy.GUID);
				if (armyGoToInstruction != null && armyGoToInstruction.IsMoving)
				{
					Diagnostics.LogWarning("Order preprocessing failed because the destination is moving. Attacker={0}, Defender={1}.", new object[]
					{
						order.GameEntityGUID,
						order.DestinationGUID
					});
					return false;
				}
			}
		}
		if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
		{
			return false;
		}
		PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
		if (pathfindingResult != null)
		{
			order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
			ArmyGoToInstruction armyGoToInstruction2 = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
			if (order.WorldPath.IsValid)
			{
				if (armyGoToInstruction2 != null && !(armyGoToInstruction2 is ArmyGoToAndMergeInstruction))
				{
					armyGoToInstruction2.Cancel(true);
					this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction2);
					armyGoToInstruction2 = null;
				}
				if (armyGoToInstruction2 == null)
				{
					armyGoToInstruction2 = new ArmyGoToAndMergeInstruction(army.GUID, gameEntityWithWorldPosition.GUID);
					this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction2);
				}
				armyGoToInstruction2.Reset(order.WorldPath);
				armyGoToInstruction2.Resume();
			}
			else if (armyGoToInstruction2 != null)
			{
				armyGoToInstruction2.Cancel(false);
			}
			order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
			return true;
		}
		return true;
	}

	private IEnumerator GoToAndMergeProcessor(OrderGoToAndMerge order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	private bool OrderGoToAndResettlePreprocessor(OrderGoToAndResettle order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army != null)
		{
			if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
			{
				return false;
			}
			bool flag = false;
			foreach (Unit unit in army.Units)
			{
				if (unit.CheckUnitAbility(UnitAbility.ReadonlyResettle, -1))
				{
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				Diagnostics.LogWarning("Order preprocessing failed because the army has not the ability to Resettle.");
				return false;
			}
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.Empire == army.Empire && region.City.BesiegingEmpire != null)
			{
				bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == army.WorldPosition);
				if (flag2)
				{
					Diagnostics.LogWarning("Order preprocessor failed because the army is in a besieged city.");
					return false;
				}
			}
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			ArmyAction value = database.GetValue(order.ArmyActionName);
			if (value == null)
			{
				Diagnostics.LogError("Order preprocessing failed because the army action '{0}' is invalid.", new object[]
				{
					order.ArmyActionName
				});
				return false;
			}
			if (!this.DepartmentOfTheInterior.CanColonizeRegion(order.Destination, value, true))
			{
				return false;
			}
			PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
			if (pathfindingResult != null)
			{
				order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
				ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
				if (order.WorldPath.IsValid)
				{
					if (armyGoToInstruction != null && !(armyGoToInstruction is ArmyGoToAndResettleInstruction))
					{
						armyGoToInstruction.Cancel(true);
						this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						armyGoToInstruction = null;
					}
					if (armyGoToInstruction == null)
					{
						armyGoToInstruction = new ArmyGoToAndResettleInstruction(army, order.ArmyActionName);
						this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
					}
					armyGoToInstruction.Reset(order.WorldPath);
					armyGoToInstruction.Resume();
				}
				else if (armyGoToInstruction != null)
				{
					armyGoToInstruction.Cancel(false);
				}
				order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
				return true;
			}
		}
		return true;
	}

	private IEnumerator OrderGoToAndResettleProcessor(OrderGoToAndResettle order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	private bool OrderGoToAndTerraformPreprocessor(OrderGoToAndTerraform order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessing failed because the army guid is not valid.");
			return false;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army != null)
		{
			if (army.WorldPosition == order.Destination || !order.Destination.IsValid)
			{
				return false;
			}
			bool flag = army.Empire.SimulationObject.Tags.Contains("FactionTraitFlames1");
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region != null && region.City != null && region.City.Empire == army.Empire && region.City.BesiegingEmpire != null)
			{
				bool flag2 = region.City.Districts.Any((District match) => match.Type != DistrictType.Exploitation && match.WorldPosition == army.WorldPosition);
				if (flag2)
				{
					Diagnostics.LogWarning("Order preprocessor failed because the army is in a besieged city.");
					return false;
				}
			}
			ITerraformDeviceService service = this.GameService.Game.Services.GetService<ITerraformDeviceService>();
			if (service == null)
			{
				Diagnostics.LogError("Cannot retreive the terraform device service.");
				return false;
			}
			if (!service.IsPositionValidForDevice(army.Empire, order.Destination))
			{
				return false;
			}
			PathfindingResult pathfindingResult = this.PathfindingService.FindPath(army, army.WorldPosition, order.Destination, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies, null);
			if (pathfindingResult != null)
			{
				order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
				ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == army.GUID);
				if (order.WorldPath.IsValid)
				{
					if (armyGoToInstruction != null && !(armyGoToInstruction is ArmyGoToAndTerraformInstruction))
					{
						armyGoToInstruction.Cancel(true);
						this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
						armyGoToInstruction = null;
					}
					if (armyGoToInstruction == null)
					{
						armyGoToInstruction = new ArmyGoToAndTerraformInstruction(army, order.ArmyActionName, order.TerraformDeviceDefinitionName);
						this.armiesWithPendingGoToInstructions.Add(armyGoToInstruction);
					}
					armyGoToInstruction.Reset(order.WorldPath);
					armyGoToInstruction.Resume();
				}
				else if (armyGoToInstruction != null)
				{
					armyGoToInstruction.Cancel(false);
				}
				order.EstimatedTimeOfArrival = global::Game.Time + 1.0 * (double)order.WorldPath.ShortestLength;
				return true;
			}
		}
		return true;
	}

	private IEnumerator OrderGoToAndTerraformProcessor(OrderGoToAndTerraform order)
	{
		yield return this.GoToProcessor(order);
		yield break;
	}

	protected bool MoveToPreprocessor(OrderMoveTo order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Preprocessor failed because game entity guid is invalid.");
			return false;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			return false;
		}
		bool flag = false;
		bool flag2 = false;
		if (army.IsLocked)
		{
			Diagnostics.LogWarning("Preprocessor failed because the army is locked. Army = {0}.", new object[]
			{
				order.GameEntityGUID
			});
			flag = true;
		}
		float propertyValue = army.GetPropertyValue(SimulationProperties.Movement);
		if (propertyValue <= 0f)
		{
			flag = true;
		}
		Diagnostics.Assert(this.GameService != null);
		Diagnostics.Assert(this.GameService.Game != null);
		Army armyAtPosition = this.WorldPositionningService.GetArmyAtPosition(order.To);
		if (armyAtPosition != null)
		{
			flag = true;
		}
		PathfindingFlags pathfindingFlags = PathfindingFlags.IgnoreFogOfWar;
		DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency != null && !agency.CanMoveOn(order.From, army.IsPrivateers, army.IsCamouflaged))
		{
			pathfindingFlags |= PathfindingFlags.IgnoreDiplomacy;
		}
		Diagnostics.Assert(this.PathfindingService != null);
		if (!this.PathfindingService.IsTilePassable(order.To, army, pathfindingFlags, null))
		{
			Diagnostics.LogWarning("Preprocessor failed because the tile {0} is not passable for army {1}.", new object[]
			{
				order.To,
				order.GameEntityGUID
			});
			flag = true;
		}
		if (!this.PathfindingService.IsTileStopable(order.To, army, pathfindingFlags, null))
		{
			Diagnostics.LogWarning("Preprocessor failed because the tile {0} is not stopable for army {1}.", new object[]
			{
				order.To,
				order.GameEntityGUID
			});
			flag = true;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.GameEntityGUID);
		if (armyGoToInstruction != null)
		{
			flag |= armyGoToInstruction.IsMoveCancelled;
		}
		order.MovementCostRatioPerUnit = new float[army.Units.Count<Unit>()];
		int num = 0;
		foreach (Unit unit2 in army.Units)
		{
			float num2 = 0f;
			if (!flag)
			{
				PathfindingContext pathfindingContext = unit2.GenerateContext();
				Diagnostics.Assert(pathfindingContext != null);
				Diagnostics.Assert(this.PathfindingService != null);
				WorldPosition start = order.From;
				if (order.IntermediatesPositions != null)
				{
					for (int i = 0; i < order.IntermediatesPositions.Length; i++)
					{
						float transitionCost = this.PathfindingService.GetTransitionCost(start, order.IntermediatesPositions[i], pathfindingContext, pathfindingFlags, null);
						float maximumMovementPoints = this.PathfindingService.GetMaximumMovementPoints(order.IntermediatesPositions[i], pathfindingContext, pathfindingFlags);
						float num3 = (maximumMovementPoints <= 0f) ? float.PositiveInfinity : (transitionCost / maximumMovementPoints);
						pathfindingContext.CurrentMovementRatio -= num3;
						num2 += num3;
						start = order.IntermediatesPositions[i];
					}
				}
				float transitionCost2 = this.PathfindingService.GetTransitionCost(start, order.To, pathfindingContext, pathfindingFlags, null);
				float maximumMovementPoints2 = this.PathfindingService.GetMaximumMovementPoints(order.To, pathfindingContext, pathfindingFlags);
				num2 += ((maximumMovementPoints2 <= 0f) ? float.PositiveInfinity : (transitionCost2 / maximumMovementPoints2));
				if (float.IsInfinity(num2) || float.IsNaN(num2))
				{
					Diagnostics.LogWarning("Preprocessor failed because the transition between {0} and {1} is impossible for army {2}.", new object[]
					{
						order.From,
						order.To,
						order.GameEntityGUID
					});
					flag = true;
				}
				else if (!flag2)
				{
					float num4 = unit2.GetPropertyValue(SimulationProperties.MovementRatio) - num2;
					if (num4 <= 0f && armyGoToInstruction != null && armyGoToInstruction.Progress < armyGoToInstruction.WorldPositions.Length)
					{
						flag2 = true;
					}
				}
			}
			if (flag)
			{
				if (armyGoToInstruction != null)
				{
					armyGoToInstruction.Cancel(true);
					this.armiesWithPendingGoToInstructions.Remove(armyGoToInstruction);
				}
				order.Canceled = true;
				return true;
			}
			order.MovementCostRatioPerUnit[num] = num2;
			num++;
		}
		if (flag2)
		{
			order.InvalidateWorldPath = true;
		}
		order.BreakSiege = false;
		order.WasBesiegingCity = false;
		order.IsBesiegingCity = false;
		order.WasDefendingCity = false;
		order.IsDefendingCity = false;
		Region region = this.WorldPositionningService.GetRegion(order.From);
		if (region != null && region.City != null)
		{
			if (region.City.BesiegingEmpire == base.Empire)
			{
				if (!this.WorldPositionningService.IsWaterTile(order.From))
				{
					order.WasBesiegingCity = region.City.Districts.Any((District match) => match.WorldPosition == order.From);
				}
				if (!this.WorldPositionningService.IsWaterTile(order.To))
				{
					order.IsBesiegingCity = region.City.Districts.Any((District match) => match.WorldPosition == order.To);
				}
				if (order.WasBesiegingCity)
				{
					order.BreakSiege = true;
					for (int j = 0; j < this.DepartmentOfDefense.Armies.Count; j++)
					{
						if (this.DepartmentOfDefense.Armies[j] != army)
						{
							District district = this.WorldPositionningService.GetDistrict(this.DepartmentOfDefense.Armies[j].WorldPosition);
							if (district != null && district.City == region.City && district.Type == DistrictType.Exploitation && !this.WorldPositionningService.IsWaterTile(district.WorldPosition))
							{
								for (int k = 0; k < region.City.Districts.Count; k++)
								{
									if ((region.City.Districts[k].Type == DistrictType.Exploitation || region.City.Districts[k].Type == DistrictType.Center) && this.PathfindingService.IsTransitionPassable(order.To, region.City.Districts[k].WorldPosition, army, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreOtherEmpireDistrict | PathfindingFlags.IgnoreDiplomacy | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreDistrict, null))
									{
										order.BreakSiege = false;
										break;
									}
								}
							}
						}
					}
				}
			}
			else if (region.City.Empire == base.Empire && region.City.BesiegingEmpire != null)
			{
				District district2 = this.WorldPositionningService.GetDistrict(order.From);
				District district3 = this.WorldPositionningService.GetDistrict(order.To);
				order.WasDefendingCity = (!this.WorldPositionningService.IsWaterTile(order.From) && district2 != null && District.IsACityTile(district2));
				order.IsDefendingCity = (!this.WorldPositionningService.IsWaterTile(order.To) && district3 != null && District.IsACityTile(district3));
				return order.WasDefendingCity == order.IsDefendingCity;
			}
			if (region.City.BesiegingSeafaringArmies.Contains(army))
			{
				order.WasBesiegingCity = true;
				if (this.WorldPositionningService.IsWaterTile(order.To))
				{
					order.IsBesiegingCity = region.City.Districts.Any((District match) => match.WorldPosition == order.To);
					if (!order.IsBesiegingCity)
					{
						order.BreakSiege = true;
					}
				}
			}
		}
		if (army.IsNaval)
		{
			region = this.WorldPositionningService.GetRegion(order.To);
			if (region != null && region.City != null && region.City.Districts.Any((District match) => match.WorldPosition == order.To))
			{
				float num5 = army.Units.Sum((Unit unit) => unit.GetPropertyValue(SimulationProperties.CityDefensePointLossPerTurn));
				if (num5 > 0f)
				{
					if (region.City.BesiegingSeafaringArmies.Exists((Army besiegingSeafaringArmy) => besiegingSeafaringArmy.Empire.Index == army.Empire.Index))
					{
						order.IsBesiegingCity = true;
					}
					else if (region.City.BesiegingEmpire != null && region.City.BesiegingEmpire.Index == army.Empire.Index)
					{
						order.IsBesiegingCity = true;
					}
				}
			}
		}
		if (army.Hero != null && this.DepartmentOfIntelligence != null)
		{
			order.BreakInfiltration = this.DepartmentOfIntelligence.CheckBreakInfiltrationOnMove(army, order.To);
		}
		if (army.PillageTarget.IsValid)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(army.PillageTarget, out gameEntity))
			{
				order.BreakPillage = true;
			}
			else
			{
				PointOfInterest pointOfInterest = gameEntity as PointOfInterest;
				int distance = this.WorldPositionningService.GetDistance(order.To, pointOfInterest.WorldPosition);
				order.BreakPillage = (distance > 1);
				if (distance == 1 && !this.PathfindingService.IsTransitionPassable(order.To, pointOfInterest.WorldPosition, army.GenerateContext().MovementCapacities, OrderAttack.AttackFlags))
				{
					order.BreakPillage = true;
				}
			}
		}
		return true;
	}

	protected IEnumerator MoveToProcessor(OrderMoveTo order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			yield break;
		}
		if (order.Canceled)
		{
			army.SetWorldPathWithEstimatedTimeOfArrival(null, global::Game.Time);
			yield break;
		}
		int unitIndex = 0;
		foreach (Unit unit in army.Units)
		{
			unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, unit.GetPropertyValue(SimulationProperties.MovementRatio) - order.MovementCostRatioPerUnit[unitIndex]);
			if (unit.GetPropertyValue(SimulationProperties.MovementRatio) < 0.01f)
			{
				unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
			}
			if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
			{
				unit.SwitchToEmbarkedUnit(this.PathfindingService.GetTileMovementCapacity(order.To, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
			}
			unitIndex++;
		}
		if (order.BreakSiege)
		{
			Region region = this.WorldPositionningService.GetRegion(army.WorldPosition);
			if (region.City != null)
			{
				DepartmentOfTheInterior departmentOfTheInterior = region.City.Empire.GetAgency<DepartmentOfTheInterior>();
				if (region.City.BesiegingEmpire == base.Empire)
				{
					departmentOfTheInterior.StopSiege(region.City);
				}
				if (region.City.BesiegingSeafaringArmies.Contains(army))
				{
					departmentOfTheInterior.StopNavalSiege(region.City, army);
				}
			}
		}
		if (order.BreakInfiltration && this.DepartmentOfIntelligence != null)
		{
			this.DepartmentOfIntelligence.StopInfiltration(army.Hero, true, true);
		}
		if (order.BreakPillage)
		{
			DepartmentOfDefense.StopPillage(army);
		}
		if (army.IsAspirating)
		{
			this.DepartmentOfDefense.StopAspirating(army);
		}
		IDatabase<SimulationDescriptor> simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		if (order.WasBesiegingCity != order.IsBesiegingCity)
		{
			SimulationDescriptor descriptor = simulationDescriptorDatabase.GetValue("ArmyStatusBesieger");
			if (army.IsNaval)
			{
				if (order.IsBesiegingCity)
				{
					Region region2 = this.WorldPositionningService.GetRegion(order.To);
					DepartmentOfTheInterior departmentOfTheInterior2 = region2.City.Empire.GetAgency<DepartmentOfTheInterior>();
					departmentOfTheInterior2.StartNavalSiege(region2.City, army);
				}
			}
			else
			{
				if (order.WasBesiegingCity)
				{
					army.RemoveDescriptor(descriptor);
				}
				else
				{
					army.SwapDescriptor(descriptor);
				}
				if (order.IsBesiegingCity || (order.WasBesiegingCity && !order.BreakSiege))
				{
					Region region3 = this.WorldPositionningService.GetRegion(army.WorldPosition);
					if (region3.City != null && region3.City.BesiegingEmpire == base.Empire)
					{
						int besiegingEmpireIndex = region3.City.BesiegingEmpireIndex;
						region3.City.BesiegingEmpireIndex = -1;
						region3.City.BesiegingEmpireIndex = besiegingEmpireIndex;
					}
				}
			}
		}
		if (order.WasDefendingCity != order.IsDefendingCity)
		{
			SimulationDescriptor descriptor2 = simulationDescriptorDatabase.GetValue("ArmyStatusCityDefender");
			if (order.WasDefendingCity)
			{
				army.RemoveDescriptor(descriptor2);
			}
			else
			{
				army.SwapDescriptor(descriptor2);
			}
		}
		if (order.InvalidateWorldPath)
		{
			army.Refresh(false);
			army.SetWorldPositionWithEstimatedTimeOfArrival(order.To, order.EstimatedTimeOfArrival);
			army.WorldPath.Rebuild(order.To, army, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, PathfindingFlags.IgnoreArmies, false);
			army.SetWorldPathWithEstimatedTimeOfArrival(army.WorldPath, order.EstimatedTimeOfArrival);
		}
		else
		{
			army.SetWorldPositionWithEstimatedTimeOfArrival(order.To, order.EstimatedTimeOfArrival);
		}
		PathfindingMovementCapacity movementCapacity = this.PathfindingService.GetTileMovementCapacity(order.From, (PathfindingFlags)0);
		PathfindingMovementCapacity movementCapacity2 = this.PathfindingService.GetTileMovementCapacity(order.To, (PathfindingFlags)0);
		if (movementCapacity == PathfindingMovementCapacity.Water && movementCapacity2 != PathfindingMovementCapacity.Water)
		{
			army.SetSails(false);
		}
		else if (movementCapacity2 == PathfindingMovementCapacity.Water && movementCapacity != PathfindingMovementCapacity.Water)
		{
			army.SetSails(true);
		}
		army.Refresh(false);
		if (this.ArmyPositionChange != null)
		{
			this.ArmyPositionChange(this, new ArmyMoveEndedEventArgs(army, order.From, order.To));
		}
		float armyTilesMovedThisTurn = army.GetPropertyValue(SimulationProperties.TilesMovedThisTurn);
		army.SetPropertyBaseValue(SimulationProperties.TilesMovedThisTurn, armyTilesMovedThisTurn + 1f);
		army.Refresh(false);
		IEventService eventService = Services.GetService<IEventService>();
		eventService.Notify(new EventWorldArmyMoveTo(army, order.From, order.To));
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService == null)
		{
			yield break;
		}
		if (downloadableContentService.IsShared(DownloadableContent19.ReadOnlyName))
		{
			this.DepartmentOfDefense.CheckArmyOnDamageableTerrain(army);
			yield break;
		}
		yield break;
	}

	protected bool ResetGoToInstructionPreprocessor(OrderResetGoToInstruction order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the game entity guid.");
			return false;
		}
		if (this.GameService == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the game service is null.");
			return false;
		}
		IEncounterRepositoryService service = this.GameService.Game.Services.GetService<IEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<Encounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((Encounter encounter) => encounter.EncounterState != EncounterState.BattleHasEnded && encounter.Contenders != null && encounter.Contenders.Exists((Contender contender) => contender.Garrison.GUID == order.GameEntityGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.DepartmentOfDefense != null);
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is null.");
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.GameEntityGUID);
		if (armyGoToInstruction == null)
		{
			return false;
		}
		if (armyGoToInstruction.IsMoving)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is already moving.");
			return false;
		}
		if (!army.IsAbleToMove)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move.");
			return false;
		}
		if (army.IsLocked)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move.");
			return false;
		}
		WorldPosition[] remainingPath = armyGoToInstruction.GetRemainingPath();
		if (remainingPath == null || remainingPath.Length < 2)
		{
			armyGoToInstruction.Cancel(false);
		}
		else
		{
			PathfindingResult pathfindingResult = new PathfindingResult(remainingPath, army, (PathfindingFlags)0);
			order.WorldPath = new WorldPath();
			order.WorldPath.Build(pathfindingResult, army.GetPropertyValue(SimulationProperties.MovementRatio), int.MaxValue, false);
			if (order.WorldPath.IsValid)
			{
				armyGoToInstruction.Reset(order.WorldPath);
			}
			else
			{
				armyGoToInstruction.Cancel(false);
			}
		}
		return true;
	}

	protected IEnumerator ResetGoToInstructionProcessor(OrderResetGoToInstruction order)
	{
		if (!order.GameEntityGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.GameEntityGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.GameEntityGUID.ToString()
			});
			yield break;
		}
		army.SetWorldPathWithEstimatedTimeOfArrival(order.WorldPath, double.MaxValue);
		yield break;
	}

	protected bool TeleportArmyPreprocessor(OrderTeleportArmy order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the game entity guid isn't valid.");
			return false;
		}
		if (this.GameService == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the game service is null.");
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.ArmyGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.DepartmentOfDefense != null);
		Army army = this.DepartmentOfDefense.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is null.");
			return false;
		}
		WorldPosition destination = order.Destination;
		if (!destination.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the destination is invalid.");
			return false;
		}
		if (!army.IsAbleToMove)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		if (army.IsLocked)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		Diagnostics.Assert(this.PathfindingService != null);
		if (!this.PathfindingService.IsTilePassable(destination, army, PathfindingFlags.IgnoreFogOfWar, null))
		{
			Diagnostics.LogError("Order preprocessor failed because the destination position is not passable. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		if (!this.PathfindingService.IsTileStopable(destination, army, PathfindingFlags.IgnoreFogOfWar, null))
		{
			Diagnostics.LogError("Order preprocessor failed because the destination position is not stopable. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.ArmyGUID);
		if (armyGoToInstruction != null)
		{
			armyGoToInstruction.Cancel(false);
		}
		if (order.NumberOfActionPointsToSpend < 0f && !StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			ArmyAction armyAction = null;
			IDatabase<ArmyAction> database = Databases.GetDatabase<ArmyAction>(false);
			if (database != null && database.TryGetValue(order.ArmyActionName, out armyAction))
			{
				order.NumberOfActionPointsToSpend = armyAction.GetCostInActionPoints();
			}
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			SimulationObjectWrapper simulationObjectWrapper = army;
			if (simulationObjectWrapper != null)
			{
				float propertyValue = simulationObjectWrapper.GetPropertyValue(SimulationProperties.MaximumNumberOfActionPoints);
				float propertyValue2 = simulationObjectWrapper.GetPropertyValue(SimulationProperties.ActionPointsSpent);
				if (order.NumberOfActionPointsToSpend > propertyValue - propertyValue2)
				{
					Diagnostics.LogWarning("Not enough action points.");
					return false;
				}
			}
		}
		return true;
	}

	protected IEnumerator TeleportArmyProcessor(OrderTeleportArmy order)
	{
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		WorldPosition startPosition = army.WorldPosition;
		if (!order.Destination.IsValid)
		{
			Diagnostics.LogError("The destination is invalid.");
			yield break;
		}
		foreach (Unit unit in army.Units)
		{
			if (!unit.SimulationObject.Tags.Contains(DownloadableContent16.SeafaringUnit))
			{
				unit.SwitchToEmbarkedUnit(this.PathfindingService.GetTileMovementCapacity(order.Destination, (PathfindingFlags)0) == PathfindingMovementCapacity.Water);
			}
		}
		army.SetWorldPositionAndTeleport(order.Destination);
		if (army.IsPillaging)
		{
			DepartmentOfDefense.StopPillage(army);
		}
		ArmyAction armyAction = null;
		bool zeroMovement = true;
		if (!StaticString.IsNullOrEmpty(order.ArmyActionName))
		{
			IDatabase<ArmyAction> armyActionDatabase = Databases.GetDatabase<ArmyAction>(false);
			if (armyActionDatabase != null && armyActionDatabase.TryGetValue(order.ArmyActionName, out armyAction) && armyAction is IArmyActionWithMovementEffect)
			{
				zeroMovement = (armyAction as IArmyActionWithMovementEffect).ZeroMovement;
			}
		}
		if (zeroMovement)
		{
			foreach (Unit unit2 in army.Units)
			{
				unit2.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
				unit2.Refresh(false);
			}
		}
		PathfindingMovementCapacity movementCapacity = this.PathfindingService.GetTileMovementCapacity(startPosition, (PathfindingFlags)0);
		PathfindingMovementCapacity movementCapacity2 = this.PathfindingService.GetTileMovementCapacity(order.Destination, (PathfindingFlags)0);
		if (movementCapacity == PathfindingMovementCapacity.Water && movementCapacity2 != PathfindingMovementCapacity.Water)
		{
			army.SetSails(false);
		}
		else if (movementCapacity2 == PathfindingMovementCapacity.Water && movementCapacity != PathfindingMovementCapacity.Water)
		{
			army.SetSails(true);
		}
		if (order.NumberOfActionPointsToSpend > 0f)
		{
			ArmyAction.SpendSomeNumberOfActionPoints(army, order.NumberOfActionPointsToSpend);
		}
		if (order.ArmyActionCooldownDuration > 0f)
		{
			ArmyActionWithCooldown.ApplyCooldown(army, order.ArmyActionCooldownDuration);
		}
		if (armyAction != null)
		{
			army.OnArmyAction(armyAction, army);
		}
		army.Refresh(false);
		army.OnMoveStop();
		yield break;
	}

	protected bool TeleportArmyToCityPreprocessor(OrderTeleportArmyToCity order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the game entity guid isn't valid.");
			return false;
		}
		if (this.GameService == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the game service is null.");
			return false;
		}
		IBattleEncounterRepositoryService service = this.GameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		if (service != null)
		{
			IEnumerable<BattleEncounter> enumerable = service;
			if (enumerable != null)
			{
				bool flag = enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(order.ArmyGUID));
				if (flag)
				{
					return false;
				}
			}
		}
		Diagnostics.Assert(this.DepartmentOfDefense != null);
		Army army = this.DepartmentOfDefense.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is null.");
			return false;
		}
		if (!order.Destination.IsValid)
		{
			Diagnostics.LogError("Order preprocessor failed because the destination is invalid.");
			return false;
		}
		if (!army.IsAbleToMove)
		{
			Diagnostics.LogWarning("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		if (army.IsLocked)
		{
			Diagnostics.LogError("Order preprocessor failed because the army is not able to move. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		IGameEntity gameEntity;
		if (!this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the city is not referenced (guid = {0:X8}).", new object[]
			{
				order.CityGUID
			});
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			Diagnostics.LogError("Order processing failed because the city is not an 'City'.");
			return false;
		}
		WorldPosition destination;
		if (!this.TryGetFirstCityTileAvailableForTeleport(city, out destination))
		{
			Diagnostics.LogError("Order preprocessor failed because there isn't any valid position to spawn. Army = {0}", new object[]
			{
				army.GUID
			});
			return false;
		}
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions.Find((ArmyGoToInstruction match) => match.ArmyGUID == order.ArmyGUID);
		if (armyGoToInstruction != null)
		{
			armyGoToInstruction.Cancel(false);
		}
		order.Destination = destination;
		return true;
	}

	protected IEnumerator TeleportArmyToCityProcessor(OrderTeleportArmyToCity order)
	{
		if (!order.ArmyGUID.IsValid)
		{
			Diagnostics.LogError("Game entity guid is not valid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		Army army = this.DepartmentOfDefense.GetArmy(order.ArmyGUID);
		if (army == null)
		{
			Diagnostics.LogError("Cannot find the army referenced by the given guid: {0}.", new object[]
			{
				order.ArmyGUID.ToString()
			});
			yield break;
		}
		if (!order.Destination.IsValid)
		{
			Diagnostics.LogError("The destination is invalid.");
			yield break;
		}
		army.SetWorldPositionAndTeleport(order.Destination);
		foreach (Unit unit in army.Units)
		{
			unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 0f);
			unit.Refresh(false);
		}
		army.Refresh(false);
		if (this.ArmyTeleportedToCity != null)
		{
			IGameEntity gameEntity;
			this.GameEntityRepositoryService.TryGetValue(order.CityGUID, out gameEntity);
			City city = gameEntity as City;
			this.ArmyTeleportedToCity(this, new ArmyTeleportedToCityEventArgs(army, city));
			yield break;
		}
		yield break;
	}

	[Service]
	private IGameService GameService { get; set; }

	[Ancillary]
	private IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	[Ancillary]
	private IPathfindingService PathfindingService { get; set; }

	[Ancillary]
	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	[Ancillary]
	private IWorldPositionningService WorldPositionningService { get; set; }

	private DepartmentOfDefense DepartmentOfDefense { get; set; }

	private DepartmentOfTheInterior DepartmentOfTheInterior { get; set; }

	private DepartmentOfIntelligence DepartmentOfIntelligence { get; set; }

	public bool TryGetFirstCityTileAvailableForTeleport(City city, out WorldPosition position)
	{
		position = WorldPosition.Invalid;
		List<WorldPosition> list = new List<WorldPosition>();
		for (int i = 0; i < city.Districts.Count; i++)
		{
			if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(city.Districts[i].WorldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water))
			{
				if (District.IsACityTile(city.Districts[i]))
				{
					list.Add(city.Districts[i].WorldPosition);
				}
			}
		}
		if (list.Count == 0)
		{
			return false;
		}
		list.Sort((WorldPosition left, WorldPosition right) => this.WorldPositionningService.GetDistance(city.WorldPosition, left).CompareTo(this.WorldPositionningService.GetDistance(city.WorldPosition, right)));
		position = list[0];
		return true;
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.GameService = Services.GetService<IGameService>();
		this.PathfindingService = this.GameService.Game.Services.GetService<IPathfindingService>();
		if (this.PathfindingService == null)
		{
			Diagnostics.LogError("Failed to retrieve the pathfinding service.");
		}
		this.PlayerControllerRepositoryService = this.GameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.PlayerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
		}
		this.GameEntityRepositoryService = this.GameService.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
		}
		this.WorldPositionningService = this.GameService.Game.Services.GetService<IWorldPositionningService>();
		if (this.WorldPositionningService == null)
		{
			Diagnostics.LogError("Failed to retrieve the world positionning service.");
		}
		base.Empire.RegisterPass("GameClientState_Turn_End", "ResetArmyMovement", new Agency.Action(this.ResetArmyMovement), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Begin", "ResetArmyGoToInstruction", new Agency.Action(this.ResetArmyGoToInstruction), new string[0]);
		base.Empire.RegisterPass("GameServerState_Turn_Finished", "ContinueGoToInstruction", new Agency.Action(this.GameServerState_Turn_Finished_ContinueGoToInstruction), new string[0]);
		this.DepartmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		this.DepartmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		this.DepartmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		this.keyMappingService = Services.GetService<IKeyMappingService>();
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		this.armiesWithPendingGoToInstructions.Clear();
		this.GameService = null;
		this.PathfindingService = null;
		this.PlayerControllerRepositoryService = null;
		this.GameEntityRepositoryService = null;
		this.WorldPositionningService = null;
		this.DepartmentOfDefense = null;
		this.DepartmentOfTheInterior = null;
		this.DepartmentOfIntelligence = null;
	}

	private IEnumerator ResetArmyGoToInstruction(string context, string name)
	{
		Diagnostics.Assert(this.armiesWithPendingGoToInstructions != null);
		for (int index = this.armiesWithPendingGoToInstructions.Count - 1; index >= 0; index--)
		{
			ArmyGoToInstruction armyGoToInstruction = this.armiesWithPendingGoToInstructions[index];
			WorldPosition[] remainingPath = armyGoToInstruction.GetRemainingPath();
			if (remainingPath == null || remainingPath.Length < 2)
			{
				if (this.DepartmentOfDefense.Armies.FirstOrDefault((Army match) => match.GUID == armyGoToInstruction.ArmyGUID) == null)
				{
					armyGoToInstruction.Cancel(true);
					this.armiesWithPendingGoToInstructions.RemoveAt(index);
				}
				else
				{
					armyGoToInstruction.Cancel(false);
				}
			}
			else if (this.DepartmentOfDefense.Armies.FirstOrDefault((Army match) => match.GUID == armyGoToInstruction.ArmyGUID) == null)
			{
				armyGoToInstruction.Cancel(true);
				this.armiesWithPendingGoToInstructions.RemoveAt(index);
			}
			else
			{
				global::Empire empire = base.Empire as global::Empire;
				OrderResetGoToInstruction order = new OrderResetGoToInstruction(base.Empire.Index, armyGoToInstruction);
				Diagnostics.Assert(empire.PlayerControllers.Server != null, "Empire player controller (server) is null.");
				empire.PlayerControllers.Server.PostOrder(order);
			}
		}
		yield break;
	}

	private IEnumerator ResetArmyMovement(string context, string name)
	{
		foreach (Army army in this.DepartmentOfDefense.Armies)
		{
			foreach (Unit unit in army.Units)
			{
				unit.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
			}
			army.Refresh(false);
		}
		if (this.DepartmentOfTheInterior != null)
		{
			foreach (City city in this.DepartmentOfTheInterior.Cities)
			{
				foreach (Unit unit2 in city.Units)
				{
					unit2.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
				}
				if (city.Camp != null)
				{
					foreach (Unit unit3 in city.Camp.Units)
					{
						unit3.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
					}
				}
				city.Refresh(false);
			}
			MajorEmpire majorEmpire = base.Empire as MajorEmpire;
			if (majorEmpire != null && majorEmpire.ConvertedVillages != null)
			{
				foreach (Village village in majorEmpire.ConvertedVillages)
				{
					foreach (Unit unit4 in village.Units)
					{
						unit4.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
					}
					village.Refresh(false);
				}
			}
			foreach (Fortress fortress in this.DepartmentOfTheInterior.OccupiedFortresses)
			{
				foreach (Unit unit5 in fortress.Units)
				{
					unit5.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
				}
				fortress.Refresh(false);
			}
		}
		DepartmentOfEducation departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		if (departmentOfEducation != null)
		{
			foreach (Unit unit6 in departmentOfEducation.Heroes)
			{
				unit6.SetPropertyBaseValue(SimulationProperties.MovementRatio, 1f);
			}
		}
		yield break;
	}

	public const int PathNumberOfTurns = 2147483647;

	private List<ArmyGoToInstruction> armiesWithPendingGoToInstructions = new List<ArmyGoToInstruction>();

	private IKeyMappingService keyMappingService;
}
