using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_QuestSolver : AILayerCommanderController, IXmlSerializable
{
	public AILayer_QuestSolver() : base("QuestSolver")
	{
		this.solverByQuest = new Dictionary<GameEntityGUID, AIQuestSolver>();
		this.toBeRemoved = new List<GameEntityGUID>();
		this.questRegions = new List<int>();
		this.QuestSolverOrders = new List<AILayer_QuestSolver.QuestSolverOrder>();
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfInternalAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfInternalAffairs>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfCreepingNodes = base.AIEntity.Empire.GetAgency<DepartmentOfCreepingNodes>();
		this.synchronousJobRepository = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.randomNumberGeneratorAIHelper = AIScheduler.Services.GetService<IRandomNumberGeneratorAIHelper>();
		this.questSolverDatabase = Databases.GetDatabase<AIQuestSolverDefinition>(false);
		this.endTurnService = Services.GetService<IEndTurnService>();
		IGameService service = Services.GetService<IGameService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.defaultSolverDefinition = this.questSolverDatabase.GetValue(AILayer_QuestSolver.DefaultQuestSolverDefinition);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_QuestSolver_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_QuestSolver_CreateLocalNeeds", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		this.DiplomacyLayer = base.AIEntity.GetLayer<AILayer_Diplomacy>();
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.eventService.EventRaise += this.EventService_EventRaise_CheckForACursedBounty;
		this.seasonService = service.Game.Services.GetService<ISeasonService>();
		Diagnostics.Assert(this.seasonService != null);
		this.seasonService.SeasonChange += this.OnSeasonChange;
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfInternalAffairs = null;
		this.departmentOfDefense = null;
		this.departmentOfTheInterior = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfCreepingNodes = null;
		this.synchronousJobRepository = null;
		this.personalityAIHelper = null;
		this.randomNumberGeneratorAIHelper = null;
		this.aiDataRepositoryHelper = null;
		this.questSolverDatabase = null;
		this.endTurnService = null;
		this.questManagementService = null;
		this.questRepositoryService = null;
		this.worldPositionningService = null;
		this.gameEntityRepositoryService = null;
		this.pathfindingService = null;
		this.ruinHuntTargets = null;
		this.DiplomacyLayer = null;
		this.QuestSolverOrders.Clear();
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise_CheckForACursedBounty;
			this.eventService = null;
		}
		if (this.seasonService != null)
		{
			this.seasonService.SeasonChange -= this.OnSeasonChange;
			this.seasonService = null;
		}
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.toBeRemoved.Clear();
		foreach (KeyValuePair<GameEntityGUID, AIQuestSolver> keyValuePair in this.solverByQuest)
		{
			Quest quest;
			if (!this.questRepositoryService.TryGetValue(keyValuePair.Key, out quest) || !this.questManagementService.IsQuestRunningForEmpire(quest.QuestDefinition.Name, base.AIEntity.Empire))
			{
				this.toBeRemoved.Add(keyValuePair.Key);
			}
		}
		for (int i = 0; i < this.toBeRemoved.Count; i++)
		{
			this.solverByQuest.Remove(this.toBeRemoved[i]);
		}
		bool flag = false;
		foreach (Quest quest2 in this.departmentOfInternalAffairs.QuestJournal.Read(QuestState.InProgress))
		{
			if (quest2 != null && quest2.QuestDefinition != null && !quest2.QuestDefinition.Tags.Contains(QuestDefinition.TagHidden))
			{
				AIQuestSolver aiquestSolver;
				if (!this.solverByQuest.TryGetValue(quest2.GUID, out aiquestSolver))
				{
					if (quest2.QuestDefinition.IsGlobal)
					{
						continue;
					}
					AIQuestSolverDefinition aiquestSolverDefinition;
					if (!this.questSolverDatabase.TryGetValue(quest2.QuestDefinition.Name, out aiquestSolverDefinition))
					{
						aiquestSolverDefinition = this.defaultSolverDefinition;
					}
					if (aiquestSolverDefinition.ChanceOfSuccess <= 0f)
					{
						continue;
					}
					aiquestSolver = new AIQuestSolver(aiquestSolverDefinition, this.ComputeDuration(aiquestSolverDefinition));
					this.solverByQuest.Add(quest2.GUID, aiquestSolver);
				}
				if (aiquestSolver != null && this.endTurnService.Turn - quest2.TurnWhenStarted >= aiquestSolver.ChosenDuration)
				{
					flag = true;
				}
			}
		}
		if (flag)
		{
			this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.ParseEndedSolvers));
		}
	}

	private bool CheckAgainstSuccesRate(AIQuestSolver solver)
	{
		float num = this.randomNumberGeneratorAIHelper.Range(0f, 1f);
		float value = this.personalityAIHelper.GetValue<float>(base.AIEntity.Empire, "AIQuestSolverDefinition/ChanceOfSuccess", solver.QuestSolverDefinition.ChanceOfSuccess);
		return num <= value;
	}

	private int ComputeDuration(AIQuestSolverDefinition definition)
	{
		int num = this.personalityAIHelper.GetValue<int>(base.AIEntity.Empire, "AIQuestSolverDefinition/MinimalTurnDuration", definition.MinimalTurnDuration);
		int value = this.personalityAIHelper.GetValue<int>(base.AIEntity.Empire, "AIQuestSolverDefinition/MaximalTurnDuration", definition.MaximalTurnDuration);
		if (num > value)
		{
			Diagnostics.LogError("{0} AILayer_QuestSolver quest {1} value {2} needs to be smaller than {3}!", new object[]
			{
				base.AIEntity.Empire,
				definition.Name,
				num,
				value
			});
			num = value;
		}
		return this.randomNumberGeneratorAIHelper.Range(num, value);
	}

	private SynchronousJobState ParseEndedSolvers()
	{
		foreach (KeyValuePair<GameEntityGUID, AIQuestSolver> keyValuePair in this.solverByQuest)
		{
			Quest quest;
			if (this.questRepositoryService.TryGetValue(keyValuePair.Key, out quest) && quest.QuestState == QuestState.InProgress && this.questManagementService.IsQuestRunningForEmpire(quest.QuestDefinition.Name, base.AIEntity.Empire))
			{
				AIQuestSolver value = keyValuePair.Value;
				if (this.endTurnService.Turn - quest.TurnWhenStarted >= value.ChosenDuration)
				{
					if (this.CheckAgainstSuccesRate(value))
					{
						this.questManagementService.ForceQuestCompletion(keyValuePair.Key, QuestState.Completed);
						if (quest.QuestDefinition.SubCategory == "Village")
						{
							IGameService service = Services.GetService<IGameService>();
							if (service != null && service.Game != null && service.Game.Services != null)
							{
								IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
								IGameEntity gameEntity;
								if (service2 != null && service2.TryGetValue(quest.QuestGiverGUID, out gameEntity))
								{
									MinorEmpire minorEmpire = (gameEntity as PointOfInterest).Region.MinorEmpire;
									BarbarianCouncil agency = minorEmpire.GetAgency<BarbarianCouncil>();
									if (minorEmpire != null && agency != null && !agency.HasAllVillagesBeenPacified)
									{
										OrderPacifyMinorFaction order = new OrderPacifyMinorFaction(base.AIEntity.Empire.Index, minorEmpire.Index, true);
										base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
									}
								}
							}
						}
					}
					else
					{
						this.questManagementService.ForceQuestCompletion(keyValuePair.Key, QuestState.Failed);
					}
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private bool TryGetQuestSolverDefinition(Quest quest, out AIQuestSolverDefinition solverDefinition)
	{
		if (this.questSolverDatabase.TryGetValue(quest.QuestDefinition.Name, out solverDefinition))
		{
			return true;
		}
		string x = string.Format("{0}-{1}-{2}", AILayer_QuestSolver.DefaultQuestSolverDefinition, quest.QuestDefinition.Category, quest.QuestDefinition.SubCategory);
		if (this.questSolverDatabase.TryGetValue(x, out solverDefinition))
		{
			return true;
		}
		x = string.Format("{0}-{1}", AILayer_QuestSolver.DefaultQuestSolverDefinition, quest.QuestDefinition.Category);
		if (this.questSolverDatabase.TryGetValue(x, out solverDefinition))
		{
			return true;
		}
		if (this.defaultSolverDefinition != null)
		{
			solverDefinition = this.defaultSolverDefinition;
			return true;
		}
		return false;
	}

	private SynchronousJobState TrySolveWingsOfRuinsStep1()
	{
		if (this.WingsOfRuinsPOI != null)
		{
			AICommander aicommander = this.aiCommanders.Find((AICommander match) => match is AICommander_Questsolver);
			IGameEntity gameEntity;
			if (aicommander == null || !aicommander.ForceArmyGUID.IsValid || !this.gameEntityRepositoryService.TryGetValue(aicommander.ForceArmyGUID, out gameEntity))
			{
				this.TrySolveWingsOfRuinsStep1_LookForAndSpawnFlyingUnit();
			}
		}
		return SynchronousJobState.Success;
	}

	private void TrySolveWingsOfRuinsStep1_LookForAndSpawnFlyingUnit()
	{
		List<City> list = new List<City>(this.departmentOfTheInterior.Cities);
		list.Sort((City left, City right) => this.worldPositionningService.GetDistance(left.WorldPosition, this.WingsOfRuinsPOI.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, this.WingsOfRuinsPOI.WorldPosition)));
		foreach (City city in list)
		{
			if (city.StandardUnits.Count > 0)
			{
				foreach (Unit unit in city.StandardUnits)
				{
					if (unit.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying" && unit.GetPropertyValue(SimulationProperties.Movement) > 1f && unit.GetPropertyValue(SimulationProperties.LevelDisplayed) >= 4f)
					{
						List<Unit> list2 = new List<Unit>();
						list2.Add(unit);
						List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city);
						int num = 0;
						if (num < availablePositionsForArmyCreation.Count)
						{
							OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(city.Empire.Index, city.GUID, list2.ConvertAll<GameEntityGUID>((Unit u) => u.GUID).ToArray(), availablePositionsForArmyCreation[num], StaticString.Empty, false, true, true);
							Ticket ticket;
							base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.TrySolveWingsOfRuinsStep1_OnCreateResponse));
							return;
						}
					}
				}
			}
		}
	}

	protected void TrySolveWingsOfRuinsStep1_OnCreateResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = args.Order as OrderTransferGarrisonToNewArmy;
			IGameEntity gameEntity;
			if (orderTransferGarrisonToNewArmy != null && orderTransferGarrisonToNewArmy.ArmyGuid.IsValid && this.gameEntityRepositoryService.TryGetValue(orderTransferGarrisonToNewArmy.ArmyGuid, out gameEntity))
			{
				Army army = gameEntity as Army;
				if (army != null)
				{
					AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(army.GUID);
					if (aidata != null && aidata.CommanderMission != null)
					{
						aidata.CommanderMission.Interrupt();
					}
					AICommander aicommander = this.aiCommanders.Find((AICommander match) => match is AICommander_Questsolver);
					if (aicommander == null)
					{
						this.AddCommander(new AICommander_Questsolver
						{
							ForceArmyGUID = army.GUID,
							SubObjectiveGuid = this.WingsOfRuinsPOI.GUID,
							Empire = base.AIEntity.Empire,
							AIPlayer = base.AIEntity.AIPlayer
						});
						return;
					}
					(aicommander as AICommander_Questsolver).SubObjectiveGuid = this.WingsOfRuinsPOI.GUID;
					IGameEntity gameEntity2;
					if (!aicommander.ForceArmyGUID.IsValid || !this.gameEntityRepositoryService.TryGetValue(aicommander.ForceArmyGUID, out gameEntity2))
					{
						aicommander.ForceArmyGUID = army.GUID;
						aicommander.Initialize();
						aicommander.Load();
						aicommander.CreateMission();
					}
				}
			}
		}
	}

	private void RefreshObjectives_TryGetWingsOfRuinsPOI()
	{
		if (this.questBehaviour.Quest.GetCurrentStepIndex() > 0)
		{
			return;
		}
		bool flag = false;
		foreach (UnitDesign unitDesign in this.departmentOfDefense.UnitDesignDatabase.UserDefinedUnitDesigns)
		{
			if (!unitDesign.UnitBodyDefinition.Tags.Contains("Hidden") && !unitDesign.Tags.Contains(DownloadableContent9.TagSolitary) && unitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying")
			{
				flag = true;
				break;
			}
		}
		if (!flag)
		{
			return;
		}
		PointOfInterest pointOfInterest = null;
		foreach (QuestBehaviourTreeNode_Action_AddQuestMarker questBehaviourTreeNode_Action_AddQuestMarker in new List<QuestBehaviourTreeNode_Action_AddQuestMarker>(Array.FindAll<BehaviourTreeNode>((this.questBehaviour.Root as BehaviourTreeNodeController).Children, (BehaviourTreeNode x) => x is QuestBehaviourTreeNode_Action_AddQuestMarker).Cast<QuestBehaviourTreeNode_Action_AddQuestMarker>()))
		{
			QuestMarker questMarker = null;
			if (this.questManagementService.TryGetMarkerByGUID(questBehaviourTreeNode_Action_AddQuestMarker.QuestMarkerGUID, out questMarker))
			{
				pointOfInterest = this.worldPositionningService.GetPointOfInterest(questMarker.WorldPosition);
				break;
			}
		}
		if (pointOfInterest != null && pointOfInterest.Region.IsLand && (pointOfInterest.CreepingNodeImprovement == null || pointOfInterest.Empire.Index != base.AIEntity.Empire.Index) && (!pointOfInterest.Region.IsRegionColonized() || pointOfInterest.Region.Owner == base.AIEntity.Empire || (this.departmentOfForeignAffairs.IsFriend(pointOfInterest.Region.Owner) && this.departmentOfForeignAffairs.CanMoveOn(pointOfInterest.Region.Index, false))) && this.departmentOfTheInterior.Cities.Count > 0)
		{
			foreach (Army army in this.departmentOfDefense.Armies)
			{
				if (!army.IsSeafaring)
				{
					if (this.pathfindingService.FindPath(army, this.departmentOfTheInterior.Cities[0].WorldPosition, pointOfInterest.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
					{
						return;
					}
					this.WingsOfRuinsPOI = pointOfInterest;
					return;
				}
			}
			return;
		}
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.questBehaviour = null;
		this.WingsOfRuinsPOI = null;
		this.questRegions.Clear();
		this.RefreshObjectives_ManageQuestSolverOrders();
		this.RefreshObjectives_ManageWingsOfRuinQuest();
		this.RefreshObjectives_ManageVictoryQuest();
		this.RefreshObjectives_ManageACursedBountyQuest();
		this.RefreshObjectives_ManageEclipseRuinHunt();
	}

	public List<int> QuestRegions
	{
		get
		{
			return this.questRegions;
		}
	}

	private SynchronousJobState HuntRuins()
	{
		if (this.ruinHuntTargets.Count > 0)
		{
			List<City> list = new List<City>(this.departmentOfTheInterior.Cities);
			list.Sort((City left, City right) => this.worldPositionningService.GetDistance(left.WorldPosition, this.ruinHuntTargets[0].WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, this.ruinHuntTargets[0].WorldPosition)));
			using (List<City>.Enumerator enumerator = list.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					City city2 = enumerator.Current;
					List<WorldPosition> availablePositionsForArmyCreation = DepartmentOfDefense.GetAvailablePositionsForArmyCreation(city2);
					if (city2.BesiegingEmpireIndex < 0 && availablePositionsForArmyCreation.Count > 0 && city2.StandardUnits.Count > 0 && AILayer_Military.AreaIsSave(city2.WorldPosition, 10, this.departmentOfForeignAffairs, false))
					{
						List<Unit> list2 = city2.StandardUnits.ToList<Unit>();
						list2.Sort(delegate(Unit left, Unit right)
						{
							bool flag = left.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying";
							bool flag2 = right.UnitDesign.UnitBodyDefinition.SubCategory == "SubCategoryFlying";
							if (flag != flag2)
							{
								if (flag)
								{
									return -1;
								}
								if (flag2)
								{
									return 1;
								}
							}
							return -1 * left.GetPropertyValue(SimulationProperties.MaximumMovementOnLand).CompareTo(right.GetPropertyValue(SimulationProperties.MaximumMovementOnLand));
						});
						Comparison<PointOfInterest> <>9__2;
						foreach (Unit unit in list2)
						{
							if (unit.GetPropertyValue(SimulationProperties.Movement) > 1f && !unit.IsSettler)
							{
								List<Unit> list3 = new List<Unit>();
								list3.Add(unit);
								List<PointOfInterest> list4 = this.ruinHuntTargets;
								Comparison<PointOfInterest> comparison;
								if ((comparison = <>9__2) == null)
								{
									comparison = (<>9__2 = ((PointOfInterest left, PointOfInterest right) => this.worldPositionningService.GetDistance(left.WorldPosition, city2.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, city2.WorldPosition))));
								}
								list4.Sort(comparison);
								OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(city2.Empire.Index, city2.GUID, list3.ConvertAll<GameEntityGUID>((Unit u) => u.GUID).ToArray(), availablePositionsForArmyCreation[0], StaticString.Empty, false, true, true);
								Ticket ticket;
								base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.HuntRuins_OnCreateResponse));
								return SynchronousJobState.Success;
							}
						}
					}
				}
			}
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Success;
	}

	protected void HuntRuins_OnCreateResponse(object sender, TicketRaisedEventArgs args)
	{
		if (args.Result == PostOrderResponse.Processed)
		{
			OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = args.Order as OrderTransferGarrisonToNewArmy;
			IGameEntity gameEntity;
			if (orderTransferGarrisonToNewArmy != null && orderTransferGarrisonToNewArmy.ArmyGuid.IsValid && this.gameEntityRepositoryService.TryGetValue(orderTransferGarrisonToNewArmy.ArmyGuid, out gameEntity))
			{
				Army army = gameEntity as Army;
				if (army != null)
				{
					AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(army.GUID);
					if (aidata != null && aidata.CommanderMission != null)
					{
						aidata.CommanderMission.Interrupt();
					}
					AICommander_RuinHunter aicommander_RuinHunter = this.aiCommanders.Find((AICommander match) => match is AICommander_RuinHunter) as AICommander_RuinHunter;
					if (aicommander_RuinHunter == null)
					{
						this.AddCommander(new AICommander_RuinHunter
						{
							ForceArmyGUID = army.GUID,
							PointOfInterests = this.ruinHuntTargets,
							Empire = base.AIEntity.Empire,
							AIPlayer = base.AIEntity.AIPlayer
						});
						return;
					}
					IGameEntity gameEntity2;
					if (!aicommander_RuinHunter.ForceArmyGUID.IsValid || !this.gameEntityRepositoryService.TryGetValue(aicommander_RuinHunter.ForceArmyGUID, out gameEntity2))
					{
						aicommander_RuinHunter.ForceArmyGUID = army.GUID;
					}
					aicommander_RuinHunter.PointOfInterests = this.ruinHuntTargets;
					aicommander_RuinHunter.Initialize();
					aicommander_RuinHunter.Load();
					aicommander_RuinHunter.CreateMission();
				}
			}
		}
	}

	protected override void RefreshCommanders(StaticString context, StaticString pass)
	{
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			AICommander aicommander = this.aiCommanders[i];
			aicommander.RefreshObjective();
			aicommander.RefreshMission();
		}
		this.GenerateNewCommander();
	}

	private void RefreshObjectives_ManageWingsOfRuinQuest()
	{
		if (this.questManagementService.IsQuestRunningForEmpire(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestWingsOfRuinName, base.AIEntity.Empire))
		{
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestWingsOfRuinName, base.AIEntity.Empire.Index);
			if (this.questBehaviour != null)
			{
				this.RefreshObjectives_TryGetWingsOfRuinsPOI();
			}
			if (this.questBehaviour.Quest.GetCurrentStepIndex() > 0)
			{
				WorldPosition position = WorldPosition.Invalid;
				bool flag = false;
				foreach (QuestBehaviourTreeNode_Action_SpawnArmy questBehaviourTreeNode_Action_SpawnArmy in new List<QuestBehaviourTreeNode_Action_SpawnArmy>(Array.FindAll<BehaviourTreeNode>((this.questBehaviour.Root as BehaviourTreeNodeController).Children, (BehaviourTreeNode x) => x is QuestBehaviourTreeNode_Action_SpawnArmy).Cast<QuestBehaviourTreeNode_Action_SpawnArmy>()))
				{
					IEnumerable<WorldPosition> enumerable = null;
					if (this.questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(questBehaviourTreeNode_Action_SpawnArmy.SpawnLocationVarName, out enumerable))
					{
						foreach (WorldPosition worldPosition in enumerable)
						{
							if (worldPosition.IsValid)
							{
								position = worldPosition;
								flag = true;
								break;
							}
						}
					}
					if (flag)
					{
						break;
					}
					if (questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations != null && questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations.Length != 0)
					{
						position = questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations[0];
						break;
					}
				}
				if (position.IsValid)
				{
					Region region = this.worldPositionningService.GetRegion(position);
					if (!region.IsRegionColonized() || region.Owner == base.AIEntity.Empire)
					{
						this.questRegions.Add(region.Index);
					}
				}
			}
		}
		bool flag2 = false;
		if (this.WingsOfRuinsPOI != null)
		{
			AICommander aicommander = this.aiCommanders.Find((AICommander match) => match is AICommander_Questsolver);
			IGameEntity gameEntity;
			if (aicommander == null || !aicommander.ForceArmyGUID.IsValid || !this.gameEntityRepositoryService.TryGetValue(aicommander.ForceArmyGUID, out gameEntity))
			{
				flag2 = true;
			}
			else
			{
				(aicommander as AICommander_Questsolver).SubObjectiveGuid = this.WingsOfRuinsPOI.GUID;
				aicommander.Initialize();
				aicommander.Load();
				aicommander.CreateMission();
			}
		}
		if (flag2)
		{
			this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.TrySolveWingsOfRuinsStep1));
		}
	}

	private void RefreshObjectives_ManageVictoryQuest()
	{
		if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter2", base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter2", base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Action_SpawnArmy questBehaviourTreeNode_Action_SpawnArmy;
			if (questBehaviour != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Action_SpawnArmy>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Action_SpawnArmy))
			{
				Region region = this.worldPositionningService.GetRegion(questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations[0]);
				if (!region.IsRegionColonized() || region.Owner == base.AIEntity.Empire)
				{
					this.questRegions.Add(region.Index);
				}
			}
		}
	}

	private void RefreshObjectives_ManageACursedBountyQuest()
	{
		this.searchACursedBountyRuin = false;
		if (this.questManagementService.IsQuestRunningForEmpire(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestACursedBountyName, base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour = this.questRepositoryService.GetQuestBehaviour(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestACursedBountyName, base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Action_AddQuestMarker questBehaviourTreeNode_Action_AddQuestMarker;
			if (questBehaviour != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Action_AddQuestMarker>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Action_AddQuestMarker))
			{
				QuestMarker questMarker = null;
				if (this.questManagementService.TryGetMarkerByGUID(questBehaviourTreeNode_Action_AddQuestMarker.QuestMarkerGUID, out questMarker) && questMarker.WorldPosition.IsValid)
				{
					List<Army> list = Intelligence.GetArmiesInRegion(this.worldPositionningService.GetRegion(questMarker.WorldPosition).Index).ToList<Army>();
					float num = 0f;
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].Empire is LesserEmpire && !list[i].IsSeafaring)
						{
							num += list[i].GetPropertyValue(SimulationProperties.MilitaryPower);
						}
					}
					float militaryPowerDif = this.DiplomacyLayer.GetMilitaryPowerDif(false);
					if (num < 100f)
					{
						this.searchACursedBountyRuin = true;
					}
					else if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
					{
						if (militaryPowerDif > num * 2f)
						{
							this.searchACursedBountyRuin = true;
						}
					}
					else if (militaryPowerDif > num * 1.5f)
					{
						this.searchACursedBountyRuin = true;
					}
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}: A Cursed Bounty running; mypower {1} tetike power {2}, ruin search allowed? {3}", new object[]
						{
							base.AIEntity.Empire,
							militaryPowerDif,
							num,
							this.searchACursedBountyRuin
						});
					}
				}
			}
		}
	}

	private void RefreshObjectives_ManageEclipseRuinHunt()
	{
		AICommander_RuinHunter aicommander_RuinHunter = this.aiCommanders.Find((AICommander match) => match is AICommander_RuinHunter) as AICommander_RuinHunter;
		IGameEntity gameEntity;
		if (aicommander_RuinHunter == null || !aicommander_RuinHunter.ForceArmyGUID.IsValid || !this.gameEntityRepositoryService.TryGetValue(aicommander_RuinHunter.ForceArmyGUID, out gameEntity) || aicommander_RuinHunter.PointOfInterests.Count == 0)
		{
			if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
			{
				this.ruinHuntTargets = this.GetRuinsToHunt();
				this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.HuntRuins));
				this.needEclipseUpdate = false;
			}
			return;
		}
		if (this.needEclipseUpdate)
		{
			this.needEclipseUpdate = false;
			Army army = gameEntity as Army;
			if (army != null && army.WorldPosition.IsValid)
			{
				List<PointOfInterest> ruinsToHunt = this.GetRuinsToHunt();
				ruinsToHunt.Sort((PointOfInterest left, PointOfInterest right) => this.worldPositionningService.GetDistance(left.WorldPosition, army.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, army.WorldPosition)));
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					string format = "ELCP {1}/{2} eclipseupdate {0}";
					object[] array = new object[3];
					array[0] = string.Join(" || ", (from o in ruinsToHunt
					select o.WorldPosition.ToString()).ToArray<string>());
					array[1] = base.AIEntity.Empire;
					array[2] = army.WorldPosition;
					Diagnostics.Log(format, array);
				}
				aicommander_RuinHunter.PointOfInterests = ruinsToHunt;
			}
		}
		aicommander_RuinHunter.Initialize();
		aicommander_RuinHunter.Load();
		aicommander_RuinHunter.CreateMission();
	}

	public bool SearchACursedBountyRuin
	{
		get
		{
			return this.searchACursedBountyRuin;
		}
	}

	private void EventService_EventRaise_CheckForACursedBounty(object sender, EventRaiseEventArgs e)
	{
		if (!this.IsActive())
		{
			return;
		}
		EventQuestUpdated eventQuestUpdated = e.RaisedEvent as EventQuestUpdated;
		if (eventQuestUpdated == null)
		{
			return;
		}
		if (eventQuestUpdated.Empire.Index == base.AIEntity.Empire.Index && eventQuestUpdated.Quest.QuestDefinition.Name == AILayer_QuestSolver.ImportantQuestNames.GlobalQuestACursedBountyName)
		{
			this.RefreshObjectives_ManageACursedBountyQuest();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(1);
		base.WriteXml(writer);
		writer.WriteStartElement("QuestSolverOrders");
		writer.WriteAttributeString<int>("Count", this.QuestSolverOrders.Count);
		for (int i = 0; i < this.QuestSolverOrders.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.QuestSolverOrders[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num > 0 && reader.IsStartElement("QuestSolverOrders"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("QuestSolverOrders");
			this.QuestSolverOrders.Clear();
			for (int i = 0; i < attribute; i++)
			{
				AILayer_QuestSolver.QuestSolverOrder questSolverOrder = new AILayer_QuestSolver.QuestSolverOrder("", GameEntityGUID.Zero, GameEntityGUID.Zero);
				reader.ReadElementSerializable<AILayer_QuestSolver.QuestSolverOrder>(ref questSolverOrder);
				if (questSolverOrder.IsValid())
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}: Loading QuestSolverOrder {1}", new object[]
						{
							base.AIEntity.Empire,
							questSolverOrder.ToString()
						});
					}
					this.QuestSolverOrders.Add(questSolverOrder);
				}
			}
			reader.ReadEndElement("QuestSolverOrders");
		}
	}

	public void AddQuestSolverOrder(StaticString name, GameEntityGUID targetGUID, GameEntityGUID armyGUID)
	{
		AILayer_QuestSolver.QuestSolverOrder questSolverOrder = new AILayer_QuestSolver.QuestSolverOrder(name, targetGUID, armyGUID);
		if (!questSolverOrder.IsValid())
		{
			return;
		}
		this.QuestSolverOrders.Add(questSolverOrder);
		IGameEntity gameEntity;
		IGameEntity gameEntity2;
		if (this.gameEntityRepositoryService.TryGetValue(armyGUID, out gameEntity) && gameEntity is Army && this.gameEntityRepositoryService.TryGetValue(targetGUID, out gameEntity2))
		{
			if (!this.CanPathToObjective(gameEntity as Army, gameEntity2 as IWorldPositionable))
			{
				return;
			}
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(armyGUID);
			if (aidata != null && aidata.CommanderMission != null)
			{
				aidata.CommanderMission.Interrupt();
			}
			AICommander aicommander = this.aiCommanders.Find((AICommander match) => match is AICommander_QuestSolverOrder && match.ForceArmyGUID == armyGUID);
			if (aicommander == null)
			{
				this.AddCommander(new AICommander_QuestSolverOrder
				{
					ForceArmyGUID = armyGUID,
					SubObjectiveGuid = targetGUID,
					Empire = base.AIEntity.Empire,
					AIPlayer = base.AIEntity.AIPlayer,
					QuestName = name
				});
				return;
			}
			AICommander_QuestSolverOrder aicommander_QuestSolverOrder = aicommander as AICommander_QuestSolverOrder;
			aicommander_QuestSolverOrder.SubObjectiveGuid = targetGUID;
			aicommander_QuestSolverOrder.QuestName = name;
			aicommander.Initialize();
			aicommander.Load();
			aicommander.CreateMission();
		}
	}

	public void RemoveQuestSolverOrder(StaticString name, GameEntityGUID armyGUID)
	{
		this.QuestSolverOrders.RemoveAll((AILayer_QuestSolver.QuestSolverOrder BTo) => BTo.questName == name && BTo.armyGUID == armyGUID);
		AICommander aicommander = this.aiCommanders.Find((AICommander match) => match is AICommander_QuestSolverOrder && match.ForceArmyGUID == armyGUID && (match as AICommander_QuestSolverOrder).QuestName == name);
		if (aicommander != null)
		{
			(aicommander as AICommander_QuestSolverOrder).ForceFinish = true;
		}
	}

	private void RefreshObjectives_ManageQuestSolverOrders()
	{
		int i;
		Predicate<AICommander> <>9__0;
		int j;
		for (i = 0; i < this.QuestSolverOrders.Count; i = j + 1)
		{
			IGameEntity gameEntity;
			IGameEntity gameEntity2;
			if (!this.questManagementService.IsQuestRunningForEmpire(this.QuestSolverOrders[i].questName, base.AIEntity.Empire))
			{
				this.QuestSolverOrders.RemoveAt(i);
				j = i;
				i = j - 1;
			}
			else if (!this.gameEntityRepositoryService.TryGetValue(this.QuestSolverOrders[i].armyGUID, out gameEntity) || !this.gameEntityRepositoryService.TryGetValue(this.QuestSolverOrders[i].objectiveGUID, out gameEntity2))
			{
				this.QuestSolverOrders.RemoveAt(i);
				j = i;
				i = j - 1;
			}
			else if (this.CanPathToObjective(gameEntity as Army, gameEntity2 as IWorldPositionable))
			{
				AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.QuestSolverOrders[i].armyGUID);
				if (aidata != null && aidata.CommanderMission != null)
				{
					if (aidata.CommanderMission.Commander is AICommander_QuestSolverOrder)
					{
						goto IL_251;
					}
					aidata.CommanderMission.Interrupt();
				}
				List<AICommander> aiCommanders = this.aiCommanders;
				Predicate<AICommander> match2;
				if ((match2 = <>9__0) == null)
				{
					match2 = (<>9__0 = ((AICommander match) => match is AICommander_QuestSolverOrder && match.ForceArmyGUID == this.QuestSolverOrders[i].armyGUID));
				}
				AICommander aicommander = aiCommanders.Find(match2);
				if (aicommander == null)
				{
					this.AddCommander(new AICommander_QuestSolverOrder
					{
						ForceArmyGUID = this.QuestSolverOrders[i].armyGUID,
						SubObjectiveGuid = this.QuestSolverOrders[i].objectiveGUID,
						Empire = base.AIEntity.Empire,
						AIPlayer = base.AIEntity.AIPlayer,
						QuestName = this.QuestSolverOrders[i].questName
					});
				}
				else
				{
					AICommander_QuestSolverOrder aicommander_QuestSolverOrder = aicommander as AICommander_QuestSolverOrder;
					aicommander_QuestSolverOrder.SubObjectiveGuid = this.QuestSolverOrders[i].objectiveGUID;
					aicommander_QuestSolverOrder.QuestName = this.QuestSolverOrders[i].questName;
					aicommander.Initialize();
					aicommander.Load();
					aicommander.CreateMission();
				}
			}
			IL_251:
			j = i;
		}
	}

	private bool CanPathToObjective(Army army, IWorldPositionable target)
	{
		return target != null && army != null && this.pathfindingService.FindPath(army, army.WorldPosition, target.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreTerraformDevices | PathfindingFlags.IgnoreKaijuGarrisons, null) != null;
	}

	private void OnSeasonChange(object sender, SeasonChangeEventArgs e)
	{
		this.needEclipseUpdate = (e.NewSeason.SeasonDefinition.SeasonType == Season.ReadOnlyHeatWave);
	}

	private List<PointOfInterest> GetRuinsToHunt()
	{
		List<PointOfInterest> list = new List<PointOfInterest>();
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		List<int> list2 = new List<int>();
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			return list;
		}
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			List<int> list3 = new List<int>();
			service.ComputeNeighbourRegions(city.Region.Index, ref list3);
			if (list3 != null)
			{
				foreach (int num in list3)
				{
					Region region = this.worldPositionningService.GetRegion(num);
					if (region.IsLand && !region.IsRegionColonized())
					{
						list2.AddOnce(num);
					}
				}
			}
			foreach (PointOfInterest item in city.Region.PointOfInterests)
			{
				if (ELCPUtilities.CanSearch(base.AIEntity.Empire, item, this.questManagementService, this.questRepositoryService))
				{
					list.Add(item);
				}
			}
		}
		foreach (Kaiju kaiju in (base.AIEntity.Empire as MajorEmpire).TamedKaijus)
		{
			if (kaiju.OnGarrisonMode())
			{
				List<int> list4 = new List<int>();
				service.ComputeNeighbourRegions(kaiju.Region.Index, ref list4);
				if (list4 != null)
				{
					foreach (int num2 in list4)
					{
						Region region2 = this.worldPositionningService.GetRegion(num2);
						if (region2.IsLand && !region2.IsRegionColonized())
						{
							list2.AddOnce(num2);
						}
					}
				}
				foreach (PointOfInterest item3 in kaiju.Region.PointOfInterests)
				{
					if (ELCPUtilities.CanSearch(base.AIEntity.Empire, item3, this.questManagementService, this.questRepositoryService))
					{
						list.Add(item3);
					}
				}
			}
		}
		if (this.departmentOfCreepingNodes != null)
		{
			foreach (CreepingNode creepingNode in this.departmentOfCreepingNodes.Nodes)
			{
				if (!creepingNode.IsUnderConstruction && (creepingNode.Region.Owner == null || creepingNode.Region.Owner.Index != base.AIEntity.Empire.Index) && ELCPUtilities.CanSearch(base.AIEntity.Empire, creepingNode.PointOfInterest, this.questManagementService, this.questRepositoryService) && this.departmentOfForeignAffairs.CanMoveOn((int)this.worldPositionningService.GetRegionIndex(creepingNode.WorldPosition), false))
				{
					list.Add(creepingNode.PointOfInterest);
				}
			}
		}
		if (list.Count == 0)
		{
			return list;
		}
		foreach (int regionIndex in list2)
		{
			PointOfInterest[] pointOfInterests2 = this.worldPositionningService.GetRegion(regionIndex).PointOfInterests;
			for (int j = 0; j < pointOfInterests2.Length; j++)
			{
				PointOfInterest item2 = pointOfInterests2[j];
				if (!list.Exists((PointOfInterest Poi) => Poi.GUID == item2.GUID) && ELCPUtilities.CanSearch(base.AIEntity.Empire, item2, this.questManagementService, this.questRepositoryService))
				{
					list.Add(item2);
				}
			}
		}
		return list;
	}

	public static string DefaultQuestSolverDefinition = "DefaultQuestSolverDefinition";

	private AIQuestSolverDefinition defaultSolverDefinition;

	private DepartmentOfInternalAffairs departmentOfInternalAffairs;

	private IEndTurnService endTurnService;

	private IPersonalityAIHelper personalityAIHelper;

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IDatabase<AIQuestSolverDefinition> questSolverDatabase;

	private IRandomNumberGeneratorAIHelper randomNumberGeneratorAIHelper;

	private Dictionary<GameEntityGUID, AIQuestSolver> solverByQuest;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	private List<GameEntityGUID> toBeRemoved;

	private DepartmentOfDefense departmentOfDefense;

	private QuestBehaviour questBehaviour;

	private PointOfInterest WingsOfRuinsPOI;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private IPathfindingService pathfindingService;

	private List<int> questRegions;

	private List<PointOfInterest> ruinHuntTargets;

	private DepartmentOfCreepingNodes departmentOfCreepingNodes;

	private bool searchACursedBountyRuin;

	private AILayer_Diplomacy DiplomacyLayer;

	private IEventService eventService;

	private List<AILayer_QuestSolver.QuestSolverOrder> QuestSolverOrders;

	private ISeasonService seasonService;

	private bool needEclipseUpdate;

	public static class ImportantQuestNames
	{
		public static readonly StaticString GlobalQuestWingsOfRuinName = new StaticString("GlobalQuestCompet#0005");

		public static readonly StaticString GlobalQuestACursedBountyName = new StaticString("GlobalQuestCompet#0001");

		public static readonly StaticString MainQuestRageWizardsChapter1Name = new StaticString("MainQuestRageWizards-Chapter1");
	}

	public class QuestSolverOrder : IXmlSerializable
	{
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteVersionAttribute(1);
			writer.WriteAttributeString<StaticString>("questName", this.questName);
			writer.WriteAttributeString<ulong>("objectiveGUID", this.objectiveGUID);
			writer.WriteAttributeString<ulong>("armyGUID", this.armyGUID);
		}

		public void ReadXml(XmlReader reader)
		{
			reader.ReadVersionAttribute();
			this.questName = reader.GetAttribute("questName");
			this.objectiveGUID = reader.GetAttribute<ulong>("objectiveGUID");
			this.armyGUID = reader.GetAttribute<ulong>("armyGUID");
			reader.ReadStartElement();
		}

		public override string ToString()
		{
			string arg = (this.questName != null) ? this.questName.ToString() : "null";
			return string.Format("{0}:{1},{2}", arg, this.objectiveGUID, this.armyGUID);
		}

		public bool IsValid()
		{
			return this.questName != "" && this.objectiveGUID != GameEntityGUID.Zero && this.armyGUID != GameEntityGUID.Zero;
		}

		public QuestSolverOrder(StaticString questname, GameEntityGUID targetGUID, GameEntityGUID armyGUID)
		{
			this.questName = questname;
			this.objectiveGUID = targetGUID;
			this.armyGUID = armyGUID;
		}

		public StaticString questName;

		public GameEntityGUID objectiveGUID;

		public GameEntityGUID armyGUID;
	}
}
