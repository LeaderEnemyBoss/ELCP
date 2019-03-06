using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

[Diagnostics.TagAttribute("AI")]
public class AILayer_ArmyRecruitment : AILayerCommanderController
{
	public AILayer_ArmyRecruitment() : base("AILayer_ArmyRecruitment")
	{
	}

	private void CreateNewCommanderRegroup(GameEntityGUID[] units, RequestUnitListMessage message)
	{
		AICommander commander = new AICommander_RegroupArmies(units, (message == null) ? 0UL : message.ID)
		{
			AIPlayer = base.AIEntity.AIPlayer,
			Empire = base.AIEntity.Empire
		};
		this.AddCommander(commander);
	}

	private void _Internal_RegroupSmallFreeArmies(Garrison garrison)
	{
		int num = 0;
		int num2 = 0;
		for (int i = 0; i < garrison.StandardUnits.Count; i++)
		{
			AIData_Unit aidata_Unit;
			if (this.aiDataRepository.TryGetAIData<AIData_Unit>(garrison.StandardUnits[i].GUID, out aidata_Unit))
			{
				if (aidata_Unit.ReservationExtraTag == AIData_Unit.AIDataReservationExtraTag.FreeForExploration)
				{
					num++;
				}
				else
				{
					num2++;
				}
			}
		}
		float num3 = (float)num / (float)garrison.MaximumUnitSlot;
		int num4 = Mathf.Max(this.minimumUnitsToKeepInGarrison, Mathf.CeilToInt((float)garrison.MaximumUnitSlot * this.unitInGarrisonMaxPercent));
		if (num3 >= this.unitInGarrisonMaxPercent)
		{
			if (num2 < num4)
			{
				num -= num4 - num2;
			}
			int num5 = 0;
			while (num5 < garrison.StandardUnits.Count && num > 0)
			{
				AIData_Unit aidata_Unit2;
				if (this.aiDataRepository.TryGetAIData<AIData_Unit>(garrison.StandardUnits[num5].GUID, out aidata_Unit2) && aidata_Unit2.ReservationExtraTag == AIData_Unit.AIDataReservationExtraTag.FreeForExploration)
				{
					num--;
					this.smallEntitiesToRegroup.Add(aidata_Unit2);
				}
				num5++;
			}
		}
	}

	private void _Internal_ResetRecruitementLocks(Garrison garrison)
	{
		for (int i = 0; i < garrison.StandardUnits.Count; i++)
		{
			AIData_Unit aidata_Unit;
			if (this.aiDataRepository.TryGetAIData<AIData_Unit>(garrison.StandardUnits[i].GUID, out aidata_Unit))
			{
				if (aidata_Unit.IsUnitLocked())
				{
					if (aidata_Unit.IsUnitLockedByMe(base.InternalGUID))
					{
						aidata_Unit.TryUnLockUnit(base.InternalGUID);
					}
				}
				else if (!this.unitsGUIDS.Contains(aidata_Unit.Unit.GUID) && !aidata_Unit.Unit.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
				{
					aidata_Unit.TagAsFreeForExploration();
				}
			}
		}
	}

	private void CleanupRequestUnitMessages()
	{
		IEnumerable<EvaluableMessage_UnitRequest> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_UnitRequest>(BlackboardLayerID.Empire);
		if (messages != null)
		{
			foreach (EvaluableMessage_UnitRequest evaluableMessage_UnitRequest in messages)
			{
				if (evaluableMessage_UnitRequest.RequestUnitListMessageID != 0UL)
				{
					BlackboardMessage message = base.AIEntity.AIPlayer.Blackboard.GetMessage(evaluableMessage_UnitRequest.RequestUnitListMessageID);
					if (message == null || message.State == BlackboardMessage.StateValue.Message_Canceled)
					{
						evaluableMessage_UnitRequest.Cancel();
					}
				}
			}
		}
	}

	private void GenerateRequestUnitMessages()
	{
		if (this.countByUnitModel.Count == 0)
		{
			return;
		}
		List<EvaluableMessage_UnitRequest> list = new List<EvaluableMessage_UnitRequest>(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_UnitRequest>(BlackboardLayerID.Empire, (EvaluableMessage_UnitRequest match) => match.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel && match.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained));
		HeuristicValue globalMotivation = new HeuristicValue(1f);
		int index;
		Predicate<EvaluableMessage_UnitRequest> <>9__1;
		int index2;
		for (index = 0; index < this.countByUnitModel.Count; index = index2 + 1)
		{
			for (int i = 0; i < this.countByUnitModel[index].Count; i++)
			{
				if (this.countByUnitModel[index].RequestArmy != null)
				{
					List<EvaluableMessage_UnitRequest> list2 = list;
					Predicate<EvaluableMessage_UnitRequest> match2;
					if ((match2 = <>9__1) == null)
					{
						match2 = (<>9__1 = delegate(EvaluableMessage_UnitRequest match)
						{
							if (this.countByUnitModel[index].UnitDesign != null)
							{
								return match.UnitDesign != null && match.UnitDesign.Model == this.countByUnitModel[index].UnitDesign.Model && match.RequestUnitListMessageID == this.countByUnitModel[index].RequestArmy.ID;
							}
							return match.UnitDesign == null && match.RequestUnitListMessageID == this.countByUnitModel[index].RequestArmy.ID;
						});
					}
					int num = list2.FindIndex(match2);
					if (num < 0)
					{
						HeuristicValue heuristicValue = new HeuristicValue(0f);
						heuristicValue.Add(this.countByUnitModel[index].RequestArmy.Priority, "Army request '{0}' priority", new object[]
						{
							this.countByUnitModel[index].RequestArmy.ID
						});
						EvaluableMessage_UnitRequest message = new EvaluableMessage_UnitRequest(globalMotivation, heuristicValue, this.countByUnitModel[index].UnitDesign, this.countByUnitModel[index].WantedUnitPatternCategory, this.countByUnitModel[index].RequestArmy.ID, -1, 1, AILayer_AccountManager.MilitaryAccountName);
						base.AIEntity.AIPlayer.Blackboard.AddMessage(message);
					}
					else
					{
						list[num].Refresh(1f, this.countByUnitModel[index].RequestArmy.Priority);
						list.RemoveAt(num);
					}
				}
			}
			index2 = index;
		}
		for (int j = 0; j < list.Count; j++)
		{
			list[j].Cancel();
		}
	}

	private bool IsCommanderStillRegrouping(RequestUnitListMessage requestUnitList)
	{
		for (int i = 0; i < this.aiCommanders.Count; i++)
		{
			AICommander_RegroupArmies aicommander_RegroupArmies = this.aiCommanders[i] as AICommander_RegroupArmies;
			if (aicommander_RegroupArmies != null && requestUnitList.ID == aicommander_RegroupArmies.RequestUnitListMessageID)
			{
				return true;
			}
		}
		return false;
	}

	private void LaunchProductionNeeds(DepartmentOfDefense departmentOfDefense, RequestUnitListMessage requestUnitListMessage)
	{
		Predicate<AILayer_ArmyRecruitment.UnitModelProductionNeeds> <>9__1;
		for (int i = 0; i < requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count; i++)
		{
			ArmyPattern.UnitPatternCategory unitPatternCategory = requestUnitListMessage.ArmyPattern.UnitPatternCategoryList[i];
			UnitDesign bestUnitDesign = this.intelligenceAIHelper.GetBestUnitDesignForNeededCategory(departmentOfDefense, unitPatternCategory);
			if (bestUnitDesign != null)
			{
				int num = this.countByUnitModel.FindIndex((AILayer_ArmyRecruitment.UnitModelProductionNeeds match) => match.UnitDesign != null && match.UnitDesign.Model == bestUnitDesign.Model && match.RequestArmy.ID == requestUnitListMessage.ID);
				if (num < 0)
				{
					num = ~num;
					this.countByUnitModel.Insert(num, new AILayer_ArmyRecruitment.UnitModelProductionNeeds(bestUnitDesign, requestUnitListMessage, 1, unitPatternCategory.Category));
				}
				else
				{
					this.countByUnitModel[num].Count++;
				}
			}
			else
			{
				List<AILayer_ArmyRecruitment.UnitModelProductionNeeds> list = this.countByUnitModel;
				Predicate<AILayer_ArmyRecruitment.UnitModelProductionNeeds> match2;
				if ((match2 = <>9__1) == null)
				{
					match2 = (<>9__1 = ((AILayer_ArmyRecruitment.UnitModelProductionNeeds match) => match.UnitDesign == null && match.RequestArmy.ID == requestUnitListMessage.ID));
				}
				int num2 = list.FindIndex(match2);
				if (num2 < 0)
				{
					num2 = ~num2;
					this.countByUnitModel.Insert(num2, new AILayer_ArmyRecruitment.UnitModelProductionNeeds(null, requestUnitListMessage, 1, unitPatternCategory.Category));
				}
				else
				{
					this.countByUnitModel[num2].Count++;
				}
			}
		}
	}

	private void RecruitArmiesUnits(RequestUnitListMessage requestUnitListMessage, Intelligence.BestRecruitementCombination bestRecruits)
	{
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.unitsGUIDS.Clear();
		for (int i = 0; i < bestRecruits.CombinationOfArmiesUnits.Count; i++)
		{
			AIData_GameEntity aidata_GameEntity = bestRecruits.CombinationOfArmiesUnits[i];
			if (aidata_GameEntity is AIData_Army)
			{
				AIData_Army aidata_Army = aidata_GameEntity as AIData_Army;
				if (aidata_Army.Army == null)
				{
					AILayer.LogWarning(string.Format("[AILayer_ArmyRecruitment.UnitAssignation] Army was not found, was it destroyed?", new object[0]));
				}
				else
				{
					if (aidata_Army.CommanderMission != null)
					{
						aidata_Army.CommanderMission.Interrupt();
					}
					for (int j = 0; j < aidata_Army.Army.StandardUnits.Count; j++)
					{
						Unit unit = aidata_Army.Army.StandardUnits[j];
						AIData_Unit aidata_Unit;
						if (service.TryGetAIData<AIData_Unit>(unit.GUID, out aidata_Unit))
						{
							this.unitsGUIDS.Add(unit.GUID);
							aidata_Unit.ClearLock();
							aidata_Unit.TryLockUnit(base.InternalGUID, base.GetType().ToString(), AIData_Unit.AIDataReservationExtraTag.ArmyRecruitment, requestUnitListMessage.Priority);
						}
					}
				}
			}
			else if (aidata_GameEntity is AIData_Unit)
			{
				AIData_Unit aidata_Unit2 = aidata_GameEntity as AIData_Unit;
				Unit unit2 = aidata_Unit2.Unit;
				if (unit2 == null || unit2.Garrison == null)
				{
					AILayer.LogWarning(string.Format("[AILayer_ArmyRecruitment.UnitAssignation] Unit was not found, was it destroyed?", new object[0]));
				}
				else
				{
					this.unitsGUIDS.Add(unit2.GUID);
					if (aidata_Unit2.IsUnitLocked() && !aidata_Unit2.IsUnitLockedByMe(base.InternalGUID))
					{
						IGarrison garrison = unit2.Garrison;
						if (garrison is Army)
						{
							Army army = garrison as Army;
							AIData_Army aidata = service.GetAIData<AIData_Army>(army.GUID);
							if (aidata == null)
							{
								AILayer.LogWarning(string.Format("[AILayer_ArmyRecruitment.UnitAssignation] Army was not found, was it destroyed?", new object[0]));
							}
							else if (aidata.CommanderMission != null)
							{
								aidata.CommanderMission.Interrupt();
							}
							else
							{
								AILayer.LogWarning(string.Format("[AILayer_ArmyRecruitment.UnitAssignation] Although unit is locked, Army commander mission was not found.", new object[0]));
								aidata_Unit2.ClearLock();
							}
						}
						else if (garrison is City)
						{
							aidata_Unit2.ClearLock();
						}
						else
						{
							aidata_Unit2.ClearLock();
						}
					}
					aidata_Unit2.TryLockUnit(base.InternalGUID, base.GetType().ToString(), AIData_Unit.AIDataReservationExtraTag.ArmyRecruitment, requestUnitListMessage.Priority);
				}
			}
			else
			{
				AILayer.LogError("[AILayer_ArmyRecruitment.UnitAssignation] Unknown GameEntity Type {0}", new object[]
				{
					aidata_GameEntity.GetType().ToString()
				});
			}
		}
		if (this.unitsGUIDS.Count != 0)
		{
			this.CreateNewCommanderRegroup(this.unitsGUIDS.ToArray(), requestUnitListMessage);
			requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Regrouping;
		}
	}

	private void RegroupSmallFreeArmies()
	{
		List<Army> list = new List<Army>();
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.smallEntitiesToRegroup.Clear();
		this.tempEntitiesToRegroup.Clear();
		int num = (int)this.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			Army army = agency.Armies[i];
			AIData_Army aidata = service.GetAIData<AIData_Army>(army.GUID);
			if (aidata != null && !aidata.IsSolitary && !aidata.Army.IsSeafaring && !aidata.Army.HasCatspaw && !(aidata.Army is KaijuArmy))
			{
				if (aidata.CommanderMission != null)
				{
					if (aidata.CommanderMission.Commander == null)
					{
						AILayer.LogError("[AILayer_ArmyRecruitment] Commander Mission without a commander");
						goto IL_131;
					}
					if (!AILayer_ArmyRecruitment.IsCommanderMissionNotInteresting(aidata.CommanderMission.Commander.Category))
					{
						goto IL_131;
					}
				}
				else if (!aidata.IsTaggedFreeForExploration())
				{
					goto IL_131;
				}
				if (army.StandardUnits.Count < num)
				{
					if (!this.IsMercArmy(aidata.Army))
					{
						this.smallEntitiesToRegroup.Add(aidata);
					}
					else
					{
						list.Add(aidata.Army);
					}
				}
			}
			IL_131:;
		}
		if (this.Empire is MajorEmpire)
		{
			DepartmentOfTheInterior agency2 = this.Empire.GetAgency<DepartmentOfTheInterior>();
			if (agency2 != null)
			{
				for (int j = 0; j < agency2.Cities.Count; j++)
				{
					City garrison = agency2.Cities[j];
					this._Internal_RegroupSmallFreeArmies(garrison);
				}
			}
			List<Village> convertedVillages = ((MajorEmpire)this.Empire).ConvertedVillages;
			for (int k = 0; k < convertedVillages.Count; k++)
			{
				Village village = convertedVillages[k];
				if (village != null)
				{
					this._Internal_RegroupSmallFreeArmies(village);
				}
			}
		}
		while (this.smallEntitiesToRegroup.Count > 1)
		{
			AIData_GameEntity aidata_GameEntity = this.smallEntitiesToRegroup[0];
			this.smallEntitiesToRegroup.RemoveAt(0);
			int num2;
			if (aidata_GameEntity is AIData_Army)
			{
				num2 = (aidata_GameEntity as AIData_Army).Army.StandardUnits.Count;
			}
			else
			{
				if (!(aidata_GameEntity is AIData_Unit))
				{
					continue;
				}
				num2 = 1;
			}
			this.tempEntitiesToRegroup.Clear();
			this.tempEntitiesToRegroup.Add(aidata_GameEntity);
			int num3 = this.smallEntitiesToRegroup.Count - 1;
			while (num3 >= 0 && num2 < num)
			{
				AIData_GameEntity item = this.smallEntitiesToRegroup[num3];
				int num4;
				if (aidata_GameEntity is AIData_Army)
				{
					num4 = (aidata_GameEntity as AIData_Army).Army.StandardUnits.Count;
					goto IL_29E;
				}
				if (aidata_GameEntity is AIData_Unit)
				{
					num4 = 1;
					goto IL_29E;
				}
				IL_296:
				num3--;
				continue;
				IL_29E:
				if (num2 + num4 <= num)
				{
					num2 += num4;
					this.tempEntitiesToRegroup.Add(item);
					this.smallEntitiesToRegroup.RemoveAt(num3);
					goto IL_296;
				}
				goto IL_296;
			}
			if (this.tempEntitiesToRegroup.Count > 1)
			{
				this.unitsGUIDS.Clear();
				for (int l = 0; l < this.tempEntitiesToRegroup.Count; l++)
				{
					AIData_GameEntity aidata_GameEntity2 = this.tempEntitiesToRegroup[l];
					if (aidata_GameEntity2 is AIData_Army)
					{
						AIData_Army aidata_Army = aidata_GameEntity2 as AIData_Army;
						if (aidata_Army.CommanderMission != null)
						{
							aidata_Army.CommanderMission.Interrupt();
						}
						for (int m = 0; m < aidata_Army.Army.StandardUnits.Count; m++)
						{
							AIData_Unit aidata_Unit;
							if (service.TryGetAIData<AIData_Unit>(aidata_Army.Army.StandardUnits[m].GUID, out aidata_Unit))
							{
								aidata_Unit.ReservationExtraTag = AIData_Unit.AIDataReservationExtraTag.None;
								this.unitsGUIDS.Add(aidata_Unit.Unit.GUID);
							}
						}
					}
					else if (aidata_GameEntity2 is AIData_Unit)
					{
						AIData_Unit aidata_Unit2 = aidata_GameEntity2 as AIData_Unit;
						aidata_Unit2.ReservationExtraTag = AIData_Unit.AIDataReservationExtraTag.None;
						this.unitsGUIDS.Add(aidata_Unit2.Unit.GUID);
					}
				}
				if (this.unitsGUIDS.Count != 0)
				{
					this.CreateNewCommanderRegroup(this.unitsGUIDS.ToArray(), null);
				}
			}
		}
		if (list.Count > 1)
		{
			int num5 = 0;
			List<GameEntityGUID> list2 = new List<GameEntityGUID>();
			for (int n = 0; n < list.Count; n++)
			{
				num5++;
				foreach (Unit unit in list[n].StandardUnits)
				{
					AIData_Unit aidata_Unit3;
					if (service.TryGetAIData<AIData_Unit>(unit.GUID, out aidata_Unit3))
					{
						aidata_Unit3.ReservationExtraTag = AIData_Unit.AIDataReservationExtraTag.None;
						list2.Add(aidata_Unit3.Unit.GUID);
						if (list2.Count >= num)
						{
							this.CreateNewCommanderRegroup(list2.ToArray(), null);
							list2.Clear();
							num5 = 0;
							break;
						}
					}
				}
			}
			if (num5 > 1)
			{
				this.CreateNewCommanderRegroup(list2.ToArray(), null);
			}
		}
	}

	private void ResetRecruitementLocks()
	{
		this.unitsGUIDS.Clear();
		foreach (AICommander aicommander in base.AICommanders)
		{
			foreach (AICommanderMission aicommanderMission in aicommander.Missions)
			{
				if (aicommanderMission.Completion == AICommanderMission.AICommanderMissionCompletion.Initializing)
				{
					AICommanderMission_RegroupArmyAt aicommanderMission_RegroupArmyAt = aicommanderMission as AICommanderMission_RegroupArmyAt;
					if (aicommanderMission_RegroupArmyAt != null)
					{
						this.unitsGUIDS.AddRange(aicommanderMission_RegroupArmyAt.UnitGuids);
					}
				}
			}
		}
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			this._Internal_ResetRecruitementLocks(agency.Armies[i]);
		}
		if (this.Empire is MajorEmpire)
		{
			DepartmentOfTheInterior agency2 = this.Empire.GetAgency<DepartmentOfTheInterior>();
			if (agency2 != null)
			{
				for (int j = 0; j < agency2.Cities.Count; j++)
				{
					this._Internal_ResetRecruitementLocks(agency2.Cities[j]);
				}
			}
			List<Village> convertedVillages = ((MajorEmpire)this.Empire).ConvertedVillages;
			for (int k = 0; k < convertedVillages.Count; k++)
			{
				Village village = convertedVillages[k];
				if (village != null)
				{
					this._Internal_ResetRecruitementLocks(village);
				}
			}
		}
	}

	private void ResetRequestArmyMessages()
	{
		List<RequestUnitListMessage> list = new List<RequestUnitListMessage>(base.AIEntity.AIPlayer.Blackboard.GetMessages<RequestUnitListMessage>(BlackboardLayerID.Empire));
		int i = 0;
		while (i < list.Count)
		{
			RequestUnitListMessage requestUnitListMessage = list[i];
			if (requestUnitListMessage.ExecutionState == RequestUnitListMessage.RequestUnitListState.Regrouping)
			{
				if (!this.IsCommanderStillRegrouping(requestUnitListMessage))
				{
					requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Pending;
					goto IL_72;
				}
				requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Regrouping;
			}
			else if (requestUnitListMessage.State != BlackboardMessage.StateValue.Message_Canceled && requestUnitListMessage.State != BlackboardMessage.StateValue.Message_Failed)
			{
				goto IL_72;
			}
			IL_69:
			i++;
			continue;
			IL_72:
			if (requestUnitListMessage.ArmyPattern == null || requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count == 0)
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(requestUnitListMessage);
				goto IL_69;
			}
			if (requestUnitListMessage is RequestArmyMessage)
			{
				(requestUnitListMessage as RequestArmyMessage).HeroGUID = GameEntityGUID.Zero;
			}
			for (int j = 0; j < requestUnitListMessage.ArmyPattern.UnitPatternCategoryList.Count; j++)
			{
				if (StaticString.IsNullOrEmpty(requestUnitListMessage.ArmyPattern.UnitPatternCategoryList[j].Category))
				{
					base.AIEntity.AIPlayer.Blackboard.CancelMessage(requestUnitListMessage);
					break;
				}
			}
			goto IL_69;
		}
	}

	private void ValidateRequestedUnitCompliance()
	{
	}

	private void VerifyAndUpdateRecruitementLocks()
	{
		IAIDataRepositoryAIHelper service = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		DepartmentOfDefense agency = this.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.Armies.Count; i++)
		{
			AIData_Unit aidata_Unit;
			for (int j = 0; j < agency.Armies[i].StandardUnits.Count; j++)
			{
				if (service.TryGetAIData<AIData_Unit>(agency.Armies[i].StandardUnits[j].GUID, out aidata_Unit) && aidata_Unit.IsUnitLocked())
				{
					if (aidata_Unit.IsUnitLockedByMe(base.InternalGUID))
					{
						aidata_Unit.TryUnLockUnit(base.InternalGUID);
					}
					else
					{
						AICommanderMission commanderMissionWithGUID = base.AIEntity.GetCommanderMissionWithGUID(aidata_Unit.ReservingGUID);
						if (commanderMissionWithGUID == null)
						{
							aidata_Unit.ClearLock();
						}
						else
						{
							aidata_Unit.ReservationPriority = commanderMissionWithGUID.Commander.GetPriority(commanderMissionWithGUID);
						}
					}
				}
			}
			if (agency.Armies[i].Hero != null && service.TryGetAIData<AIData_Unit>(agency.Armies[i].Hero.GUID, out aidata_Unit) && aidata_Unit.IsUnitLocked())
			{
				aidata_Unit.ClearLock();
			}
		}
	}

	private global::Empire Empire
	{
		get
		{
			return base.AIEntity.Empire;
		}
	}

	public static WorldPosition GetValidArmySpawningPosition(Army army, IWorldPositionningService worldPositionningService, IPathfindingService pathfindingService)
	{
		WorldOrientation worldOrientation = WorldOrientation.East;
		for (int i = 0; i < 6; i++)
		{
			WorldPosition neighbourTile = worldPositionningService.GetNeighbourTile(army.WorldPosition, worldOrientation, 1);
			if (neighbourTile.IsValid && !worldPositionningService.IsWaterTile(neighbourTile) && pathfindingService.IsTransitionPassable(army.WorldPosition, neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null) && pathfindingService.IsTileStopableAndPassable(neighbourTile, army, PathfindingFlags.IgnoreFogOfWar, null))
			{
				return neighbourTile;
			}
			worldOrientation = worldOrientation.Rotate(1);
		}
		return WorldPosition.Invalid;
	}

	public static bool IsCommanderMissionNotInteresting(AICommanderMissionDefinition.AICommanderCategory category)
	{
		return category == AICommanderMissionDefinition.AICommanderCategory.Patrol || category == AICommanderMissionDefinition.AICommanderCategory.Exploration || category == AICommanderMissionDefinition.AICommanderCategory.WarPatrol;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.unitInGarrisonMaxPercent = this.personalityAIHelper.GetRegistryValue<float>(this.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "UnitInGarrisonMaxPercent"), this.unitInGarrisonMaxPercent);
		this.minimumUnitsToKeepInGarrison = this.personalityAIHelper.GetRegistryValue<int>(this.Empire, string.Format("{0}/{1}", AILayer_Military.RegistryPath, "MinimumUnitsToKeepInGarrison"), this.minimumUnitsToKeepInGarrison);
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayerArmyRecruitment_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		IEnumerable<BlackboardMessage> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<BlackboardMessage>(BlackboardLayerID.Empire);
		if (messages != null)
		{
			IUnitDesignDatabase agency = this.Empire.GetAgency<DepartmentOfDefense>();
			using (IEnumerator<BlackboardMessage> enumerator = messages.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					BlackboardMessage blackboardMessage = enumerator.Current;
					if (blackboardMessage is EvaluableMessageWithUnitDesign)
					{
						(blackboardMessage as EvaluableMessageWithUnitDesign).Load(this.Empire, agency);
					}
				}
				yield break;
			}
		}
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.countByUnitModel.Clear();
		this.intelligenceAIHelper = null;
		this.worldPositionningService = null;
		this.personalityAIHelper = null;
		this.aiDataRepository = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		AILayer_Military layer = base.AIEntity.GetLayer<AILayer_Military>();
		base.CreateLocalNeeds(context, pass);
		this.VerifyAndUpdateRecruitementLocks();
		Diagnostics.Assert(base.AIEntity != null && base.AIEntity.AIPlayer != null && base.AIEntity.AIPlayer.Blackboard != null);
		IEnumerable<RequestUnitListMessage> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<RequestUnitListMessage>(BlackboardLayerID.Empire);
		Diagnostics.Assert(messages != null);
		List<RequestUnitListMessage> list = new List<RequestUnitListMessage>(messages);
		Diagnostics.Assert(this.Empire != null);
		list.RemoveAll((RequestUnitListMessage match) => match.EmpireTarget != this.Empire.Index);
		this.ResetRequestArmyMessages();
		this.countByUnitModel.Clear();
		list.Sort((RequestUnitListMessage left, RequestUnitListMessage right) => -1 * left.Priority.CompareTo(right.Priority));
		this.intelligenceAIHelper.FillAvailableUnitDesignList(this.Empire);
		Diagnostics.Assert(this.intelligenceAIHelper != null);
		for (int i = 0; i < list.Count; i++)
		{
			RequestUnitListMessage requestUnitListMessage = list[i];
			if (float.IsNaN(requestUnitListMessage.Priority) || float.IsInfinity(requestUnitListMessage.Priority))
			{
				AILayer.LogWarning("[SCORING] Skipping RequestUnitListMessage {0} with a priority of {1}", new object[]
				{
					requestUnitListMessage.CommanderCategory,
					requestUnitListMessage.Priority
				});
			}
			else if (AILayer_ArmyRecruitment.IsCommanderMissionNotInteresting(requestUnitListMessage.CommanderCategory))
			{
				requestUnitListMessage.State = BlackboardMessage.StateValue.Message_Canceled;
				requestUnitListMessage.TimeOut = 0;
			}
			else
			{
				BlackboardMessage.StateValue state = requestUnitListMessage.State;
				if (state != BlackboardMessage.StateValue.Message_None)
				{
					if (state == BlackboardMessage.StateValue.Message_Success || state == BlackboardMessage.StateValue.Message_Canceled)
					{
						goto IL_1F3;
					}
					if (state != BlackboardMessage.StateValue.Message_InProgress)
					{
						AILayer.LogError("[AILayer_ArmyRecruitment] Unknow state for RequestArmyMessage {0}", new object[]
						{
							requestUnitListMessage.State
						});
						goto IL_1F3;
					}
				}
				if (requestUnitListMessage.ExecutionState == RequestUnitListMessage.RequestUnitListState.Pending)
				{
					Intelligence.BestRecruitementCombination bestRecruitementCombination;
					if (requestUnitListMessage is RequestGarrisonMessage)
					{
						bestRecruitementCombination = null;
					}
					else if (requestUnitListMessage is RequestGarrisonCampMessage)
					{
						bestRecruitementCombination = null;
					}
					else
					{
						bestRecruitementCombination = this.intelligenceAIHelper.FillArmyPattern(this.Empire.Index, requestUnitListMessage, layer);
					}
					if (bestRecruitementCombination != null)
					{
						this.RecruitArmiesUnits(requestUnitListMessage, bestRecruitementCombination);
					}
				}
			}
			IL_1F3:;
		}
		this.ResetRecruitementLocks();
		this.RegroupSmallFreeArmies();
		this.CleanupRequestUnitMessages();
	}

	public void CreateNewCommanderRegroup(GameEntityGUID[] units)
	{
		AICommander commander = new AICommander_RegroupArmies(units, 0UL)
		{
			AIPlayer = base.AIEntity.AIPlayer,
			Empire = base.AIEntity.Empire
		};
		this.AddCommander(commander);
	}

	public bool IsMercArmy(Army army)
	{
		if (army.IsPrivateers)
		{
			return true;
		}
		using (IEnumerator<Unit> enumerator = army.StandardUnits.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (!enumerator.Current.UnitDesign.Tags.Contains(TradableUnit.ReadOnlyMercenary))
				{
					return false;
				}
			}
		}
		return true;
	}

	private List<AILayer_ArmyRecruitment.UnitModelProductionNeeds> countByUnitModel = new List<AILayer_ArmyRecruitment.UnitModelProductionNeeds>();

	private List<AIData_GameEntity> smallEntitiesToRegroup = new List<AIData_GameEntity>();

	private List<AIData_GameEntity> tempEntitiesToRegroup = new List<AIData_GameEntity>();

	private List<GameEntityGUID> unitsGUIDS = new List<GameEntityGUID>();

	private IAIDataRepositoryAIHelper aiDataRepository;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private int minimumUnitsToKeepInGarrison = 2;

	private IPersonalityAIHelper personalityAIHelper;

	private float unitInGarrisonMaxPercent = 0.8f;

	private IWorldPositionningService worldPositionningService;

	private List<Army> list;

	private class UnitModelProductionNeeds
	{
		public UnitModelProductionNeeds(UnitDesign unitDesign, RequestUnitListMessage requestArmy, int count, StaticString seekedUnitPatternCategory)
		{
			this.Count = count;
			this.RequestArmy = requestArmy;
			this.UnitDesign = unitDesign;
			this.WantedUnitPatternCategory = seekedUnitPatternCategory;
		}

		public int Count;

		public RequestUnitListMessage RequestArmy;

		public UnitDesign UnitDesign;

		public StaticString WantedUnitPatternCategory;
	}
}
