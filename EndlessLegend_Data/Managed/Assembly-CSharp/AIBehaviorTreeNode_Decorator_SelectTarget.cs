using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class AIBehaviorTreeNode_Decorator_SelectTarget : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_SelectTarget()
	{
		this.TargetListVarName = string.Empty;
		this.TypeOfTarget = AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Army;
		this.TypeOfDiplomaticRelation = "Any";
		this.Output_TargetVarName = string.Empty;
		this.QuestTarget = false;
	}

	[XmlAttribute]
	public bool Inverted { get; set; }

	[XmlAttribute]
	public string Output_TargetVarName { get; set; }

	[XmlAttribute]
	public string TargetListVarName { get; set; }

	[XmlAttribute]
	public string TypeOfDiplomaticRelation { get; set; }

	[XmlAttribute]
	public AIBehaviorTreeNode_Decorator_SelectTarget.TargetType TypeOfTarget { get; set; }

	[XmlAttribute]
	public string TypeOfDiplomaticRelationVariableName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (this.TypeOfDiplomaticRelation == "VillageQuest" && (!(aiBehaviorTree.AICommander.Empire is MajorEmpire) || !aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfScience>().CanParley()))
		{
			return State.Failure;
		}
		if (this.DiplomacyLayer == null && aiBehaviorTree.AICommander.Empire is MajorEmpire)
		{
			AIEntity_Empire entity = aiBehaviorTree.AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.DiplomacyLayer = entity.GetLayer<AILayer_Diplomacy>();
		}
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (!aiBehaviorTree.Variables.ContainsKey(this.TargetListVarName))
		{
			return State.Failure;
		}
		List<IWorldPositionable> list = aiBehaviorTree.Variables[this.TargetListVarName] as List<IWorldPositionable>;
		if (list == null || list.Count == 0)
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		List<IWorldPositionable> list2 = null;
		if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Army)
		{
			list2 = list.FindAll((IWorldPositionable match) => match is Army);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin)
		{
			list2 = this.Execute_GetRuins(aiBehaviorTree, army, service, list);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Village)
		{
			list2 = this.Execute_GetVillages(aiBehaviorTree, army, service, list);
		}
		else if (this.TypeOfTarget > AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Village && this.TypeOfTarget < AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.VolcanoformerDevice)
		{
			list2 = this.Execute_GetKaijus(aiBehaviorTree, army, service, list);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.VolcanoformerDevice)
		{
			list2 = this.Execute_GetVolcanoformers(aiBehaviorTree, army, service, list);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Fortress)
		{
			list2 = this.Execute_GetFortresses(aiBehaviorTree, army, service, list);
		}
		else if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Any)
		{
			list2 = new List<IWorldPositionable>(list);
		}
		if (army.Empire is MinorEmpire || army.Empire is NavalEmpire)
		{
			for (int i = list2.Count - 1; i >= 0; i--)
			{
				Garrison garrison = list2[i] as Garrison;
				if (garrison != null && garrison.Hero != null && garrison.Hero.IsSkillUnlocked("HeroSkillLeaderMap07"))
				{
					list2.RemoveAt(i);
				}
			}
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		IWorldPositionningService service3 = service.Game.Services.GetService<IWorldPositionningService>();
		bool flag = false;
		if (this.TypeOfDiplomaticRelation == "VillageQuest")
		{
			flag = true;
		}
		if (!string.IsNullOrEmpty(this.TypeOfDiplomaticRelationVariableName) && aiBehaviorTree.Variables.ContainsKey(this.TypeOfDiplomaticRelationVariableName))
		{
			this.TypeOfDiplomaticRelation = (aiBehaviorTree.Variables[this.TypeOfDiplomaticRelationVariableName] as string);
		}
		DepartmentOfForeignAffairs departmentOfForeignAffairs = null;
		bool canAttack = false;
		if (this.TypeOfDiplomaticRelation == "Enemy")
		{
			departmentOfForeignAffairs = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
			canAttack = true;
		}
		else if (this.TypeOfDiplomaticRelation == "DangerForMe")
		{
			departmentOfForeignAffairs = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
			canAttack = false;
		}
		for (int j = list2.Count - 1; j >= 0; j--)
		{
			if (!AIBehaviorTreeNode_Decorator_SelectTarget.ValidateTarget(army, list2[j] as IGameEntity, departmentOfForeignAffairs, canAttack, service2, service3))
			{
				list2.RemoveAt(j);
			}
			else if (list2[j] is IGarrison && departmentOfForeignAffairs != null && this.DiplomacyLayer != null && (list2[j] as IGarrison).Empire is MajorEmpire && this.DiplomacyLayer.GetPeaceWish((list2[j] as IGarrison).Empire.Index))
			{
				if (!(list2[j] is Army) || !(list2[j] as Army).IsPrivateers)
				{
					list2.RemoveAt(j);
				}
			}
			else if (flag && list2[j] is Village && !this.ValidQuestVillage(list2[j] as Village, army))
			{
				list2.RemoveAt(j);
			}
		}
		IWorldPositionningService worldPositionService = service.Game.Services.GetService<IWorldPositionningService>();
		if (list2 != null && list2.Count != 0)
		{
			bool flag2;
			if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Ruin || this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.Fortress)
			{
				flag2 = army.SimulationObject.Tags.Contains("MovementCapacitySail");
			}
			else
			{
				flag2 = army.HasSeafaringUnits();
			}
			Diagnostics.Assert(worldPositionService != null);
			if (!flag2)
			{
				list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsWaterTile(element.WorldPosition));
			}
			if (army.IsSeafaring)
			{
				list2.RemoveAll((IWorldPositionable element) => !worldPositionService.IsWaterTile(element.WorldPosition));
				list2.RemoveAll((IWorldPositionable element) => worldPositionService.IsFrozenWaterTile(element.WorldPosition));
			}
		}
		if (list2 != null && list2.Count != 0)
		{
			if (list2.Count > 1)
			{
				list2.Sort((IWorldPositionable left, IWorldPositionable right) => worldPositionService.GetDistance(left.WorldPosition, army.WorldPosition).CompareTo(worldPositionService.GetDistance(right.WorldPosition, army.WorldPosition)));
			}
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetVarName] = list2[0];
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetVarName, list2[0]);
			}
		}
		else if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetVarName))
		{
			aiBehaviorTree.Variables.Remove(this.Output_TargetVarName);
		}
		State result;
		if (this.Inverted)
		{
			if (list2 != null && list2.Count != 0)
			{
				result = State.Failure;
			}
			else
			{
				result = State.Success;
			}
		}
		else if (list2 != null && list2.Count != 0)
		{
			result = State.Success;
		}
		else
		{
			aiBehaviorTree.ErrorCode = 10;
			result = State.Failure;
		}
		return result;
	}

	private static bool ValidateTarget(Army myArmy, IGameEntity gameEntity, DepartmentOfForeignAffairs departmentOfForeignAffairs, bool canAttack, IGameEntityRepositoryService gameEntityRepositoryService, IWorldPositionningService worldPositionningService)
	{
		if (gameEntity == null)
		{
			return false;
		}
		if (!gameEntityRepositoryService.Contains(gameEntity.GUID))
		{
			return false;
		}
		if (departmentOfForeignAffairs != null)
		{
			IGarrison garrison = gameEntity as IGarrison;
			if (gameEntity is Kaiju)
			{
				garrison = (gameEntity as Kaiju).GetActiveTroops();
				gameEntity = garrison;
			}
			IWorldPositionable worldPositionable = gameEntity as IWorldPositionable;
			Region region = worldPositionningService.GetRegion(worldPositionable.WorldPosition);
			if (garrison == null || worldPositionable == null)
			{
				return false;
			}
			if (canAttack)
			{
				if (!departmentOfForeignAffairs.CanAttack(gameEntity) || garrison.Empire.Index == myArmy.Empire.Index)
				{
					return false;
				}
			}
			else if (!departmentOfForeignAffairs.IsEnnemy(garrison.Empire))
			{
				return false;
			}
			if ((garrison.Empire is MinorEmpire || garrison is Village) && region != null && region.IsRegionColonized() && departmentOfForeignAffairs != null && departmentOfForeignAffairs.IsEnnemy(region.Owner))
			{
				return false;
			}
		}
		Army army = gameEntity as Army;
		if (army != null)
		{
			if (myArmy.Empire is LesserEmpire && !(army.Empire is MajorEmpire))
			{
				return false;
			}
			District district = worldPositionningService.GetDistrict(army.WorldPosition);
			if (district != null && district.Type != DistrictType.Exploitation && army.Empire == district.Empire)
			{
				return false;
			}
			if (!myArmy.HasSeafaringUnits() && army.IsNaval)
			{
				return false;
			}
		}
		return true;
	}

	private bool ValidQuestVillage(Village village, Army army)
	{
		IGameService service = Services.GetService<IGameService>();
		bool result;
		if (service == null || service.Game == null)
		{
			result = false;
		}
		else
		{
			PointOfInterest pointOfInterest = village.PointOfInterest;
			if (pointOfInterest == null)
			{
				result = false;
			}
			else if (village.HasBeenConverted || village.HasBeenPacified || village.IsInEncounter || village.PointOfInterest.PointOfInterestImprovement == null)
			{
				result = false;
			}
			else if (pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionParley"))
			{
				result = false;
			}
			else if (pointOfInterest.SimulationObject.Tags.Contains(Village.DissentedVillage))
			{
				result = false;
			}
			else if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
			{
				result = false;
			}
			else
			{
				if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) != 0)
				{
					using (IEnumerator<QuestMarker> enumerator = service.Game.Services.GetService<IQuestManagementService>().GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
					{
						while (enumerator.MoveNext())
						{
							if (enumerator.Current.IsVisibleFor(army.Empire))
							{
								return false;
							}
						}
					}
					return true;
				}
				result = true;
			}
		}
		return result;
	}

	private bool CanSearchQuest(Army army, IWorldPositionable item, IQuestManagementService questManagementService, IQuestRepositoryService questRepositoryService)
	{
		PointOfInterest pointOfInterest = item as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != ELCPUtilities.QuestLocation && pointOfInterest.Type != "NavalQuestLocation")
		{
			return false;
		}
		if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
		{
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != army.Empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return false;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return false;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return false;
				}
			}
		}
		else if (pointOfInterest.CreepingNodeImprovement != null && pointOfInterest.Empire.Index != army.Empire.Index)
		{
			return false;
		}
		if (!pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionSearch"))
		{
			foreach (QuestMarker questMarker in questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID))
			{
				Quest quest;
				if (questRepositoryService.TryGetValue(questMarker.QuestGUID, out quest))
				{
					QuestBehaviour questBehaviour = questRepositoryService.GetQuestBehaviour(quest.Name, army.Empire.Index);
					if (questBehaviour != null && quest.EmpireBits == army.Empire.Bits && questMarker.IsVisibleFor(army.Empire))
					{
						string a = quest.QuestDefinition.Name;
						if (quest.QuestDefinition.Name == AILayer_QuestSolver.ImportantQuestNames.GlobalQuestACursedBountyName && questMarker.IsVisibleInFogOfWar)
						{
							if (this.QuestLayer == null)
							{
								GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
								AIPlayer_MajorEmpire aiplayer_MajorEmpire;
								if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(army.Empire as MajorEmpire, out aiplayer_MajorEmpire))
								{
									AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
									if (entity != null)
									{
										this.QuestLayer = entity.GetLayer<AILayer_QuestSolver>();
									}
								}
							}
							if (this.QuestLayer.SearchACursedBountyRuin)
							{
								return true;
							}
							this.CursedBountyPosition = questMarker.WorldPosition;
							return false;
						}
						else
						{
							if (a == "VictoryQuest-Chapter3" && questMarker.IsVisibleInFogOfWar)
							{
								return true;
							}
							if (a == "VictoryQuest-Chapter1Alt" || a == "VictoryQuest-Chapter1")
							{
								QuestBehaviourTreeNode_ConditionCheck_Prerequisite questBehaviourTreeNode_ConditionCheck_Prerequisite;
								if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_Prerequisite>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_Prerequisite))
								{
									return false;
								}
								foreach (QuestBehaviourPrerequisites questBehaviourPrerequisites in questBehaviourTreeNode_ConditionCheck_Prerequisite.Prerequisites)
								{
									for (int j = 0; j < questBehaviourPrerequisites.Prerequisites.Length; j++)
									{
										InterpreterPrerequisite interpreterPrerequisite = questBehaviourPrerequisites.Prerequisites[j] as InterpreterPrerequisite;
										if (interpreterPrerequisite != null && !interpreterPrerequisite.Check(army))
										{
											return false;
										}
									}
								}
								return true;
							}
							else if (a == "GlobalQuestCoop#0004")
							{
								if (quest.QuestDefinition.Variables.First((QuestVariableDefinition p) => p.VarName == "$NameOfStrategicResourceToGather1") != null)
								{
									QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount questBehaviourTreeNode_ConditionCheck_HasResourceAmount;
									if (!ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_ConditionCheck_HasResourceAmount>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_ConditionCheck_HasResourceAmount))
									{
										return false;
									}
									string resourceName = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.ResourceName;
									int wantedAmount = questBehaviourTreeNode_ConditionCheck_HasResourceAmount.WantedAmount;
									DepartmentOfTheTreasury agency2 = army.Empire.GetAgency<DepartmentOfTheTreasury>();
									float num;
									if (agency2 != null && agency2.TryGetResourceStockValue(army.Empire.SimulationObject, resourceName, out num, false) && num >= (float)(wantedAmount * 3))
									{
										return true;
									}
								}
							}
						}
					}
				}
			}
			return false;
		}
		return false;
	}

	[XmlAttribute]
	public bool QuestTarget { get; set; }

	private List<IWorldPositionable> Execute_GetRuins(AIBehaviorTree aiBehaviorTree, Army army, IGameService gameService, List<IWorldPositionable> unfilteredTargetList)
	{
		IQuestManagementService questManagementService = gameService.Game.Services.GetService<IQuestManagementService>();
		IQuestRepositoryService questRepositoryService = gameService.Game.Services.GetService<IQuestRepositoryService>();
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		list = unfilteredTargetList.FindAll((IWorldPositionable match) => this.CanSearch(army, match, questManagementService, questRepositoryService));
		if (army.Empire is MajorEmpire && !army.HasCatspaw && aiBehaviorTree.AICommander.AIPlayer.AIState != AIPlayer.PlayerState.EmpireControlledByHuman)
		{
			this.CursedBountyPosition = WorldPosition.Invalid;
			list.AddRange(unfilteredTargetList.FindAll((IWorldPositionable match) => this.CanSearchQuest(army, match, questManagementService, questRepositoryService)));
			if (this.CursedBountyPosition.IsValid)
			{
				list.RemoveAll((IWorldPositionable match) => match.WorldPosition == this.CursedBountyPosition);
			}
		}
		return list;
	}

	private List<IWorldPositionable> Execute_GetVillages(AIBehaviorTree aiBehaviorTree, Army army, IGameService gameService, List<IWorldPositionable> unfilteredTargetList)
	{
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		for (int i = 0; i < unfilteredTargetList.Count; i++)
		{
			PointOfInterest pointOfInterest = unfilteredTargetList[i] as PointOfInterest;
			if (pointOfInterest != null && !(pointOfInterest.Type != "Village") && pointOfInterest.Region != null && pointOfInterest.Region.MinorEmpire != null)
			{
				BarbarianCouncil agency = pointOfInterest.Region.MinorEmpire.GetAgency<BarbarianCouncil>();
				if (agency != null)
				{
					Village villageAt = agency.GetVillageAt(pointOfInterest.WorldPosition);
					if (villageAt != null && !villageAt.HasBeenPacified && this.TypeOfDiplomaticRelation != "VillageConvert" && pointOfInterest.PointOfInterestImprovement != null && !villageAt.HasBeenInfected)
					{
						list.Add(villageAt);
					}
					else if (this.TypeOfDiplomaticRelation == "VillageConvert" && army.Empire is MajorEmpire && army.Empire.SimulationObject.Tags.Contains(AILayer_Village.TagConversionTrait) && villageAt.HasBeenPacified && DepartmentOfTheInterior.IsArmyAbleToConvert(army, true) && !villageAt.HasBeenConverted && !villageAt.HasBeenInfected)
					{
						DepartmentOfForeignAffairs agency2 = aiBehaviorTree.AICommander.Empire.GetAgency<DepartmentOfForeignAffairs>();
						City city = villageAt.Region.City;
						if (city != null && city.Empire != aiBehaviorTree.AICommander.Empire)
						{
							DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(city.Empire);
							if (diplomaticRelation == null || diplomaticRelation.State.Name != DiplomaticRelationState.Names.War || (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War && pointOfInterest.PointOfInterestImprovement == null))
							{
								goto IL_1FB;
							}
						}
						float num;
						army.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(army.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
						if (AILayer_Village.GetVillageConversionCost(army.Empire as MajorEmpire, villageAt) * 2f < num)
						{
							list.Add(villageAt);
						}
					}
				}
			}
			IL_1FB:;
		}
		return list;
	}

	private List<IWorldPositionable> Execute_GetKaijus(AIBehaviorTree aiBehaviorTree, Army army, IGameService gameService, List<IWorldPositionable> unfilteredTargetList)
	{
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		for (int i = 0; i < unfilteredTargetList.Count; i++)
		{
			Kaiju kaiju = null;
			if (unfilteredTargetList[i] is Kaiju)
			{
				kaiju = (unfilteredTargetList[i] as Kaiju);
			}
			else if (unfilteredTargetList[i] is KaijuArmy)
			{
				kaiju = (unfilteredTargetList[i] as KaijuArmy).Kaiju;
			}
			else if (unfilteredTargetList[i] is KaijuGarrison)
			{
				kaiju = (unfilteredTargetList[i] as KaijuGarrison).Kaiju;
			}
			if (kaiju != null)
			{
				if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.StunnedKaiju && !kaiju.IsTamed() && kaiju.IsStunned())
				{
					list.Add(kaiju);
				}
				if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.WildKaiju && !kaiju.IsTamed() && kaiju.IsWild())
				{
					list.Add(kaiju);
				}
				if (this.TypeOfTarget == AIBehaviorTreeNode_Decorator_SelectTarget.TargetType.TamedKaiju && kaiju.IsTamed())
				{
					list.Add(kaiju);
				}
			}
		}
		return list;
	}

	private List<IWorldPositionable> Execute_GetVolcanoformers(AIBehaviorTree aiBehaviorTree, Army army, IGameService gameService, List<IWorldPositionable> unfilteredTargetList)
	{
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		List<IWorldPositionable> result;
		using (IEnumerator<KeyValuePair<ulong, TerraformDevice>> enumerator = (gameService.Game.Services.GetService<ITerraformDeviceService>() as TerraformDeviceManager).GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				KeyValuePair<ulong, TerraformDevice> keyValuePair = enumerator.Current;
				list.Add(keyValuePair.Value);
			}
			result = list;
		}
		return result;
	}

	private List<IWorldPositionable> Execute_GetFortresses(AIBehaviorTree aiBehaviorTree, Army army, IGameService gameService, List<IWorldPositionable> unfilteredTargetList)
	{
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		for (int i = 0; i < unfilteredTargetList.Count; i++)
		{
			PointOfInterest pointOfInterest = unfilteredTargetList[i] as PointOfInterest;
			Fortress fortressAt = null;
			if (pointOfInterest != null && (pointOfInterest.Type == Fortress.Citadel || pointOfInterest.Type == Fortress.Facility))
			{
				fortressAt = pointOfInterest.Region.NavalEmpire.GetAgency<PirateCouncil>().GetFortressAt(pointOfInterest.WorldPosition);
			}
			else
			{
				fortressAt = (unfilteredTargetList[i] as Fortress);
			}
			if (fortressAt != null && !list.Exists((IWorldPositionable f) => f.WorldPosition == fortressAt.WorldPosition))
			{
				list.Add(fortressAt);
			}
		}
		return list;
	}

	private bool CanSearch(Army army, IWorldPositionable item, IQuestManagementService questManagementService, IQuestRepositoryService questRepositoryService)
	{
		if (army.HasCatspaw)
		{
			return false;
		}
		PointOfInterest pointOfInterest = item as PointOfInterest;
		if (pointOfInterest == null)
		{
			return false;
		}
		if (pointOfInterest.Type != ELCPUtilities.QuestLocation && pointOfInterest.Type != "NavalQuestLocation")
		{
			return false;
		}
		if (pointOfInterest.Interaction.IsLocked(army.Empire.Index, "ArmyActionSearch"))
		{
			return false;
		}
		if (ELCPUtilities.UseELCPPeacefulCreepingNodes)
		{
			if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != army.Empire)
			{
				if (pointOfInterest.Empire == null)
				{
					return false;
				}
				if (!(pointOfInterest.Empire is MajorEmpire))
				{
					return false;
				}
				DepartmentOfForeignAffairs agency = army.Empire.GetAgency<DepartmentOfForeignAffairs>();
				if (agency == null)
				{
					return false;
				}
				if (!agency.IsFriend(pointOfInterest.Empire))
				{
					return false;
				}
			}
		}
		else if (pointOfInterest.CreepingNodeGUID != GameEntityGUID.Zero && pointOfInterest.Empire != army.Empire)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits && !SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag))
		{
			return false;
		}
		if (SimulationGlobal.GlobalTagsContains(SeasonManager.RuinDustDepositsTag) && !pointOfInterest.UntappedDustDeposits && (pointOfInterest.Interaction.Bits & army.Empire.Bits) == army.Empire.Bits)
		{
			return false;
		}
		if ((pointOfInterest.Interaction.Bits & army.Empire.Bits) != 0)
		{
			using (IEnumerator<QuestMarker> enumerator = questManagementService.GetMarkersByBoundTargetGUID(pointOfInterest.GUID).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Quest quest;
					if (questRepositoryService.TryGetValue(enumerator.Current.QuestGUID, out quest) && quest.EmpireBits == army.Empire.Bits)
					{
						return false;
					}
				}
			}
			return true;
		}
		return true;
	}

	private AILayer_Diplomacy DiplomacyLayer;

	private AILayer_QuestSolver QuestLayer;

	private WorldPosition CursedBountyPosition;

	public enum TargetType
	{
		Army,
		Ruin,
		Village,
		WildKaiju,
		StunnedKaiju,
		TamedKaiju,
		Any = 7,
		VolcanoformerDevice = 6,
		Fortress = 8
	}
}
