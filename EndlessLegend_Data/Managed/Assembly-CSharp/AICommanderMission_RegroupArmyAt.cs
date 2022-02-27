using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_RegroupArmyAt : AICommanderMission, IXmlSerializable
{
	public override void ReadXml(XmlReader reader)
	{
		this.SourceGuid = reader.GetAttribute<ulong>("SourceGuid");
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		this.UnitGuids = new GameEntityGUID[attribute];
		if (attribute > 0)
		{
			reader.ReadStartElement("Units");
			for (int i = 0; i < this.UnitGuids.Length; i++)
			{
				this.UnitGuids[i] = reader.ReadElementString<ulong>("Unit");
			}
			reader.ReadEndElement();
		}
		else
		{
			reader.Skip();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("SourceGuid", this.SourceGuid);
		base.WriteXml(writer);
		writer.WriteStartElement("Units");
		writer.WriteAttributeString<int>("Count", this.UnitGuids.Length);
		for (int i = 0; i < this.UnitGuids.Length; i++)
		{
			writer.WriteElementString<ulong>("Unit", this.UnitGuids[i]);
		}
		writer.WriteEndElement();
	}

	public AICommander_RegroupArmies AICommanderRegroupArmies
	{
		get
		{
			return base.Commander as AICommander_RegroupArmies;
		}
	}

	public bool IsMaster
	{
		get
		{
			return this.AICommanderRegroupArmies.MasterMission == this;
		}
	}

	public float GetUnitsToRegroupMilitaryPower()
	{
		float num = 0f;
		for (int i = 0; i < this.UnitGuids.Length; i++)
		{
			AIData_Unit aidata_Unit;
			this.aiDataRepository.TryGetAIData<AIData_Unit>(this.UnitGuids[i], out aidata_Unit);
			if (aidata_Unit != null && aidata_Unit.Unit != null)
			{
				num += aidata_Unit.Unit.GetPropertyValue(SimulationProperties.MilitaryPower);
			}
		}
		return num;
	}

	public WorldPosition GetUnitsToRegroupPosition()
	{
		for (int i = 0; i < this.UnitGuids.Length; i++)
		{
			AIData_Unit aidata_Unit;
			this.aiDataRepository.TryGetAIData<AIData_Unit>(this.UnitGuids[i], out aidata_Unit);
			if (aidata_Unit != null && aidata_Unit.Unit != null && aidata_Unit.Unit.Garrison != null)
			{
				if (aidata_Unit.Unit.Garrison is Army)
				{
					return (aidata_Unit.Unit.Garrison as Army).WorldPosition;
				}
				if (aidata_Unit.Unit.Garrison is City)
				{
					return (aidata_Unit.Unit.Garrison as City).WorldPosition;
				}
				if (aidata_Unit.Unit.Garrison is Village)
				{
					return (aidata_Unit.Unit.Garrison as Village).WorldPosition;
				}
				if (aidata_Unit.Unit.Garrison is Camp)
				{
					return (aidata_Unit.Unit.Garrison as Camp).WorldPosition;
				}
				Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Unknow garrison for Unit {0}", new object[]
				{
					this.UnitGuids[i].ToString()
				});
			}
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander aiCommander)
	{
		base.Initialize(aiCommander);
		IGameService service = Services.GetService<IGameService>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.orderExecuted = false;
	}

	public override void Load()
	{
		base.Load();
		List<Unit> list = new List<Unit>();
		float num = float.MaxValue;
		float num2 = float.MaxValue;
		float num3 = float.MaxValue;
		for (int i = 0; i < this.UnitGuids.Length; i++)
		{
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(this.UnitGuids[i], out gameEntity) && gameEntity is Unit)
			{
				Unit unit = gameEntity as Unit;
				list.Add(unit);
				float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumMovement);
				if (propertyValue < num)
				{
					num = propertyValue;
				}
				float propertyValue2 = unit.GetPropertyValue(SimulationProperties.MaximumMovementOnLand);
				if (propertyValue2 < num2)
				{
					num2 = propertyValue2;
				}
				float propertyValue3 = unit.GetPropertyValue(SimulationProperties.MaximumMovementOnWater);
				if (propertyValue3 < num3)
				{
					num3 = propertyValue3;
				}
			}
		}
		this.pathfindingContext = new PathfindingContext(this.SourceGuid, base.Commander.Empire, list);
		this.pathfindingContext.RefreshProperties(1f, num, false, false, num2, num3);
		this.pathfindingContext.Greedy = true;
		this.majorEmpireAIEntity = (base.Commander.AIPlayer.AIEntities.Find((AIEntity match) => match is AIEntity_Empire) as AIEntity_Empire);
	}

	public override void Release()
	{
		base.Release();
		if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
		{
			RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
			if (requestUnitListMessage != null)
			{
				requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Pending;
			}
		}
		this.majorEmpireAIEntity = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		if (parameters.Length != 2)
		{
			Diagnostics.LogError("[AICommanderMission_RegroupArmyAt] Wrong number of parameters {0}", new object[]
			{
				parameters.Length
			});
		}
		this.SourceGuid = (GameEntityGUID)parameters[0];
		this.UnitGuids = (parameters[1] as GameEntityGUID[]);
	}

	protected override void Fail()
	{
		base.Fail();
		if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
		{
			RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
			if (requestUnitListMessage != null)
			{
				requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Pending;
			}
		}
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army armyData, out TickableState tickableState)
	{
		tickableState = this.State;
		float propertyValue = armyData.Army.GetPropertyValue(SimulationProperties.Movement);
		if (propertyValue <= 0f)
		{
			this.State = TickableState.NoTick;
			this.SetRequestMessageExecutionState(RequestUnitListMessage.RequestUnitListState.RegroupingPending);
		}
		return AICommanderMission.AICommanderMissionCompletion.Initializing;
	}

	protected override void Pending()
	{
		base.Pending();
		if (this.currentTicket != null)
		{
			return;
		}
		if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
		{
			RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
			if (requestUnitListMessage != null)
			{
				requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Pending;
			}
		}
		this.State = TickableState.NoTick;
	}

	protected override void Running()
	{
		base.Running();
		if (this.State == TickableState.NoTick)
		{
			this.SetRequestMessageExecutionState(RequestUnitListMessage.RequestUnitListState.RegroupingPending);
		}
	}

	protected override void Success()
	{
		base.Success();
		if (this.AICommanderRegroupArmies.MissionHasAllUnits(this) && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
			Diagnostics.Assert(aidata != null);
			RequestArmyMessage requestArmyMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestArmyMessage;
			if (requestArmyMessage != null)
			{
				requestArmyMessage.ArmyGUID = aidata.Army.GUID;
				requestArmyMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.ArmyAvailable;
				requestArmyMessage.TimeOut = 1;
				if (requestArmyMessage.HeroGUID != GameEntityGUID.Zero)
				{
					OrderChangeHeroAssignment order = new OrderChangeHeroAssignment(base.Commander.Empire.Index, requestArmyMessage.HeroGUID, aidata.Army.GUID);
					Ticket ticket;
					base.Commander.Empire.PlayerControllers.Client.PostOrder(order, out ticket, null);
				}
				if (this.majorEmpireAIEntity != null)
				{
					AICommanderMissionWithRequestArmy commanderMissionBasedOnItsArmyRequestArmy = this.majorEmpireAIEntity.GetCommanderMissionBasedOnItsArmyRequestArmy(requestArmyMessage.ID);
					if (commanderMissionBasedOnItsArmyRequestArmy != null)
					{
						base.SetArmyFree();
						this.Process();
						return;
					}
				}
			}
			else
			{
				RequestGarrisonMessage requestGarrisonMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestGarrisonMessage;
				if (requestGarrisonMessage != null)
				{
					requestGarrisonMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.ArmyAvailable;
					requestGarrisonMessage.TimeOut = 1;
					this.TransferUnits(requestGarrisonMessage.CityGuid);
				}
				RequestGarrisonCampMessage requestGarrisonCampMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestGarrisonCampMessage;
				if (requestGarrisonCampMessage != null)
				{
					requestGarrisonCampMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.ArmyAvailable;
					requestGarrisonCampMessage.TimeOut = 1;
					this.TransferUnits(requestGarrisonCampMessage.CampGuid);
				}
			}
		}
		base.SetArmyFree();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		if (aidata.Army.IsLocked || aidata.Army.IsInEncounter)
		{
			return false;
		}
		if (this.AICommanderRegroupArmies.MissionHasAllUnits(this))
		{
			RequestGarrisonMessage requestGarrisonMessage = null;
			if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
			{
				requestGarrisonMessage = (base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestGarrisonMessage);
			}
			if (requestGarrisonMessage == null)
			{
				RequestGarrisonCampMessage requestGarrisonCampMessage = null;
				if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
				{
					requestGarrisonCampMessage = (base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestGarrisonCampMessage);
				}
				if (requestGarrisonCampMessage == null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					return false;
				}
				IGameEntity gameEntity;
				if (!this.gameEntityRepositoryService.TryGetValue(requestGarrisonCampMessage.CampGuid, out gameEntity))
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				Camp camp = gameEntity as Camp;
				if (camp == null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				if (!this.AICommanderRegroupArmies.FinalPosition.IsValid)
				{
					this.AICommanderRegroupArmies.FinalPosition = camp.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonCampMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(this.AICommanderRegroupArmies.FinalPosition);
				if (armyAtPosition != null && armyAtPosition.GUID != base.AIDataArmyGUID)
				{
					this.AICommanderRegroupArmies.FinalPosition = camp.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonCampMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				bool flag = false;
				for (int i = 0; i < camp.Districts.Count; i++)
				{
					if (camp.Districts[i].Type != DistrictType.Exploitation && camp.Districts[i].Type != DistrictType.Improvement)
					{
						if (this.worldPositionningService.GetDistance(this.AICommanderRegroupArmies.FinalPosition, camp.Districts[i].WorldPosition) <= 1)
						{
							flag = true;
							break;
						}
					}
				}
				if (!flag)
				{
					this.AICommanderRegroupArmies.FinalPosition = camp.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonCampMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				if (aidata.Army.WorldPosition == this.AICommanderRegroupArmies.FinalPosition)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					return false;
				}
			}
			else
			{
				IGameEntity gameEntity2;
				if (!this.gameEntityRepositoryService.TryGetValue(requestGarrisonMessage.CityGuid, out gameEntity2))
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				City city = gameEntity2 as City;
				if (city == null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				if (city.IsInEncounter)
				{
					return false;
				}
				if (city.BesiegingEmpire != null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				if (!this.AICommanderRegroupArmies.FinalPosition.IsValid)
				{
					this.AICommanderRegroupArmies.FinalPosition = city.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				Army armyAtPosition2 = this.worldPositionningService.GetArmyAtPosition(this.AICommanderRegroupArmies.FinalPosition);
				if (armyAtPosition2 != null && armyAtPosition2.GUID != base.AIDataArmyGUID)
				{
					this.AICommanderRegroupArmies.FinalPosition = city.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				bool flag2 = false;
				for (int j = 0; j < city.Districts.Count; j++)
				{
					if (city.Districts[j].Type != DistrictType.Exploitation && city.Districts[j].Type != DistrictType.Improvement)
					{
						if (this.worldPositionningService.GetDistance(this.AICommanderRegroupArmies.FinalPosition, city.Districts[j].WorldPosition) <= 1)
						{
							flag2 = true;
							break;
						}
					}
				}
				if (!flag2)
				{
					this.AICommanderRegroupArmies.FinalPosition = city.GetValidDistrictToTarget(aidata.Army).WorldPosition;
					requestGarrisonMessage.FinalPosition = this.AICommanderRegroupArmies.FinalPosition;
				}
				if (aidata.Army.WorldPosition == this.AICommanderRegroupArmies.FinalPosition)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					return false;
				}
			}
		}
		if (this.targetTransferArmy != null)
		{
			if (this.targetTransferArmy.Army == null)
			{
				this.targetTransferArmy = null;
			}
			else
			{
				if (!this.targetTransferArmy.Army.IsInEncounter && !this.targetTransferArmy.Army.IsLocked)
				{
					return false;
				}
				this.targetTransferArmy = null;
			}
		}
		foreach (AICommanderMission aicommanderMission in this.AICommanderRegroupArmies.Missions)
		{
			AICommanderMission_RegroupArmyAt aicommanderMission_RegroupArmyAt = (AICommanderMission_RegroupArmyAt)aicommanderMission;
			if (aicommanderMission_RegroupArmyAt != this)
			{
				if (aicommanderMission_RegroupArmyAt.targetTransferArmy == aidata)
				{
					if (!aidata.Army.IsLocked && !aidata.Army.IsInEncounter)
					{
						return false;
					}
					aicommanderMission_RegroupArmyAt.targetTransferArmy = null;
				}
			}
		}
		if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.AIPlayer != null && this.AICommanderRegroupArmies.AIPlayer.AIEntities != null)
		{
			AIEntity_Empire aientity_Empire = this.AICommanderRegroupArmies.AIPlayer.AIEntities.Find((AIEntity match) => match is AIEntity_Empire) as AIEntity_Empire;
			if (aientity_Empire != null)
			{
				AICommanderMission_PrivateersHarass aicommanderMission_PrivateersHarass = aientity_Empire.GetCommanderMissionBasedOnItsArmyRequestArmy(this.AICommanderRegroupArmies.RequestUnitListMessageID) as AICommanderMission_PrivateersHarass;
				if (aicommanderMission_PrivateersHarass != null && aicommanderMission_PrivateersHarass.TargetCity != null && !aidata.Army.IsPrivateers && base.TryCreateArmyMission("ConvertToPrivateers", new List<object>
				{
					aicommanderMission_PrivateersHarass.TargetCity
				}))
				{
					this.State = TickableState.NeedTick;
					RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
					if (requestUnitListMessage != null)
					{
						requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Regrouping;
					}
					return true;
				}
			}
		}
		WorldPosition worldPosition;
		if (this.IsMaster)
		{
			if (this.IsArmyBesiegingACity(base.AIDataArmyGUID))
			{
				return false;
			}
			if (this.CanTransferToNearMission())
			{
				return false;
			}
			AICommanderMission_RegroupArmyAt aicommanderMission_RegroupArmyAt2 = null;
			int num = int.MaxValue;
			foreach (AICommanderMission aicommanderMission2 in this.AICommanderRegroupArmies.Missions)
			{
				AICommanderMission_RegroupArmyAt aicommanderMission_RegroupArmyAt3 = (AICommanderMission_RegroupArmyAt)aicommanderMission2;
				if (aicommanderMission_RegroupArmyAt3 != this)
				{
					WorldPosition unitsToRegroupPosition = aicommanderMission_RegroupArmyAt3.GetUnitsToRegroupPosition();
					if (unitsToRegroupPosition.IsValid)
					{
						int distance = this.worldPositionningService.GetDistance(aidata.Army.WorldPosition, unitsToRegroupPosition);
						if (distance < num)
						{
							num = distance;
							aicommanderMission_RegroupArmyAt2 = aicommanderMission_RegroupArmyAt3;
						}
					}
				}
			}
			if (aicommanderMission_RegroupArmyAt2 == null)
			{
				if (!this.AICommanderRegroupArmies.FinalPosition.IsValid)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					return false;
				}
				worldPosition = this.AICommanderRegroupArmies.FinalPosition;
			}
			else
			{
				worldPosition = aicommanderMission_RegroupArmyAt2.GetUnitsToRegroupPosition();
			}
		}
		else
		{
			if (this.CanTransferToNearMission())
			{
				return false;
			}
			AICommanderMission_RegroupArmyAt masterMission = this.AICommanderRegroupArmies.MasterMission;
			if (masterMission == null)
			{
				return false;
			}
			worldPosition = masterMission.GetUnitsToRegroupPosition();
		}
		this.State = TickableState.NoTick;
		if (aidata.Army.GetPropertyValue(SimulationProperties.Movement) > 0f)
		{
			PathfindingContext pathfindingContext = aidata.Army.GenerateContext();
			pathfindingContext.Greedy = true;
			PathfindingResult pathfindingResult = this.pathfindingService.FindPath(pathfindingContext, aidata.Army.WorldPosition, worldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreFogOfWar, null);
			if (pathfindingResult != null)
			{
				foreach (WorldPosition worldPosition2 in pathfindingResult.GetCompletePath())
				{
					if (!(worldPosition2 == pathfindingResult.Start) && !(worldPosition2 == worldPosition))
					{
						if (this.pathfindingService.IsTileStopable(worldPosition2, aidata.Army, (PathfindingFlags)0, null) && base.TryCreateArmyMission("ReachPosition", new List<object>
						{
							worldPosition2
						}))
						{
							this.State = TickableState.NeedTick;
							this.SetRequestMessageExecutionState(RequestUnitListMessage.RequestUnitListState.Regrouping);
							return true;
						}
					}
				}
			}
		}
		return false;
	}

	protected override bool TryGetArmyData()
	{
		if (this.createFirstArmyTicket != null)
		{
			return false;
		}
		if (base.AIDataArmyGUID.IsValid)
		{
			return true;
		}
		if (this.currentTicket != null)
		{
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(this.SourceGuid, out gameEntity))
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		for (int i = this.UnitGuids.Length - 1; i >= 0; i--)
		{
			AIData_Unit aidata_Unit;
			this.aiDataRepository.TryGetAIData<AIData_Unit>(this.UnitGuids[i], out aidata_Unit);
			if (aidata_Unit == null)
			{
				Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] no AIData for Unit {0}", new object[]
				{
					this.UnitGuids[i].ToString()
				});
			}
			else
			{
				if (aidata_Unit.IsUnitLocked() && !aidata_Unit.IsUnitLockedByMe(base.InternalGUID))
				{
					Diagnostics.LogWarning(string.Format("LOCKING: Unit shouldn't be in use, current unit state: {0}", aidata_Unit.GetLockingStateString()));
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				if (aidata_Unit.Unit.UnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
				{
					Diagnostics.LogWarning("You cannot regroup Colossus or Solitary unit with something else.");
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
			}
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		if (this.AICommanderRegroupArmies != null && this.AICommanderRegroupArmies.RequestUnitListMessageID != 0UL)
		{
			RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
			if (requestUnitListMessage == null)
			{
				base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
				return false;
			}
			requestUnitListMessage.ExecutionState = RequestUnitListMessage.RequestUnitListState.Regrouping;
		}
		bool flag = false;
		for (int j = 0; j < this.UnitGuids.Length; j++)
		{
			if (!garrison.ContainsUnit(this.UnitGuids[j]))
			{
				flag = true;
				break;
			}
		}
		if (flag)
		{
			List<GameEntityGUID> list = new List<GameEntityGUID>();
			for (int k = 0; k < this.UnitGuids.Length; k++)
			{
				if (garrison.ContainsUnit(this.UnitGuids[k]))
				{
					list.Add(this.UnitGuids[k]);
				}
			}
			this.UnitGuids = list.ToArray();
		}
		if (this.UnitGuids.Length == 0)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		WorldPosition armyPosition = WorldPosition.Invalid;
		Army army = gameEntity as Army;
		if (army != null)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(gameEntity.GUID);
			Diagnostics.Assert(aidata != null);
			if (army.GetPropertyValue(SimulationProperties.Movement) <= 0f)
			{
				return false;
			}
			if (aidata.CommanderMission != null)
			{
				aidata.CommanderMission.Interrupt();
			}
			if (aidata.Army.IsInEncounter || aidata.Army.IsLocked)
			{
				return false;
			}
			if (aidata.Army.StandardUnits.Count != this.UnitGuids.Length)
			{
				armyPosition = AILayer_ArmyRecruitment.GetValidArmySpawningPosition(aidata.Army, this.worldPositionningService, this.pathfindingService);
			}
			else
			{
				if (!aidata.AssignCommanderMission(this))
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
					return false;
				}
				base.AIDataArmyGUID = gameEntity.GUID;
				return true;
			}
		}
		else if (gameEntity is Village)
		{
			Village village = gameEntity as Village;
			if (village.IsInEncounter)
			{
				return false;
			}
			WorldPosition worldPosition = village.WorldPosition;
			WorldOrientation worldOrientation = WorldOrientation.East;
			if (this.AICommanderRegroupArmies.FinalPosition.IsValid)
			{
				worldOrientation = this.worldPositionningService.GetOrientation(worldPosition, this.AICommanderRegroupArmies.FinalPosition);
			}
			for (int l = 0; l < 6; l++)
			{
				WorldPosition neighbourTile = this.worldPositionningService.GetNeighbourTile(worldPosition, worldOrientation, 1);
				if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(neighbourTile, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water) && this.pathfindingService.IsTransitionPassable(village.WorldPosition, neighbourTile, this.pathfindingContext, PathfindingFlags.IgnoreFogOfWar, null) && this.pathfindingService.IsTilePassable(neighbourTile, this.pathfindingContext, (PathfindingFlags)0, null) && this.pathfindingService.IsTileStopable(neighbourTile, this.pathfindingContext, (PathfindingFlags)0, null))
				{
					Army armyAtPosition = this.worldPositionningService.GetArmyAtPosition(neighbourTile);
					if (armyAtPosition == null)
					{
						armyPosition = neighbourTile;
						break;
					}
				}
				else
				{
					worldOrientation = worldOrientation.Rotate(1);
				}
			}
		}
		else if (gameEntity is City)
		{
			City city = gameEntity as City;
			if (city.IsInEncounter)
			{
				return false;
			}
			if (!DepartmentOfTheInterior.TryGetWorldPositionForNewArmyFromCity(city, this.pathfindingService, this.pathfindingContext, out armyPosition))
			{
				return false;
			}
		}
		else
		{
			Diagnostics.LogError(string.Format("[AICommanderMission_RegroupArmyAt] The garrison {0} is not valid.", garrison.GUID));
		}
		if (armyPosition.IsValid)
		{
			if (army != null && army.WorldPath != null && army.WorldPath.Destination != army.WorldPosition)
			{
				OrderGoTo orderGoTo = new OrderGoTo(army.Empire.Index, army.GUID, army.WorldPosition);
				orderGoTo.Flags = (PathfindingFlags)0;
				base.Commander.Empire.PlayerControllers.AI.PostOrder(orderGoTo, out this.currentTicket, new EventHandler<TicketRaisedEventArgs>(this.MoveOrder_TicketRaised));
				if (this.currentTicket != null)
				{
					return false;
				}
				if (!this.orderExecuted)
				{
					Diagnostics.LogError("[AICommanderMission_RegroupArmyAt] new cancelation move order was not executed, from {0} to {1}", new object[]
					{
						army.WorldPosition,
						army.WorldPosition
					});
					return false;
				}
			}
			for (int m = 0; m < this.UnitGuids.Length; m++)
			{
				AIData_Unit aidata_Unit;
				this.aiDataRepository.TryGetAIData<AIData_Unit>(this.UnitGuids[m], out aidata_Unit);
				if (aidata_Unit == null)
				{
					Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] no AIData for Unit {0}", new object[]
					{
						this.UnitGuids[m].ToString()
					});
				}
				else if (!aidata_Unit.IsUnitLockedByMe(base.InternalGUID))
				{
					aidata_Unit.TryLockUnit(base.InternalGUID, base.GetType().ToString(), AIData_Unit.AIDataReservationExtraTag.Regrouping, base.Commander.GetPriority(this));
				}
			}
			OrderTransferGarrisonToNewArmy order = new OrderTransferGarrisonToNewArmy(base.Commander.Empire.Index, this.SourceGuid, this.UnitGuids, armyPosition, "Regroup", false, true, true);
			base.Commander.Empire.PlayerControllers.Server.PostOrder(order, out this.createFirstArmyTicket, new EventHandler<TicketRaisedEventArgs>(this.OrderTransferUnitToNewArmyTicketRaised));
		}
		return false;
	}

	private bool CanTransferToNearMission()
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null || aidata.Army.IsLocked || aidata.Army.IsInEncounter)
		{
			return false;
		}
		foreach (AICommanderMission aicommanderMission in this.AICommanderRegroupArmies.Missions)
		{
			AICommanderMission_RegroupArmyAt aicommanderMission_RegroupArmyAt = (AICommanderMission_RegroupArmyAt)aicommanderMission;
			AIData_Army aidata2 = this.aiDataRepository.GetAIData<AIData_Army>(aicommanderMission_RegroupArmyAt.AIDataArmyGUID);
			if (aicommanderMission_RegroupArmyAt != this && aicommanderMission_RegroupArmyAt.AIDataArmyGUID.IsValid && aidata2 != null && aidata2.Army != null && !aidata2.Army.IsLocked && !aidata2.Army.IsInEncounter && aicommanderMission_RegroupArmyAt.targetTransferArmy == null)
			{
				if (this.worldPositionningService.GetDistance(aidata.Army.WorldPosition, aidata2.Army.WorldPosition) == 1 && this.pathfindingService.IsTransitionPassable(aidata.Army.WorldPosition, aidata2.Army.WorldPosition, aidata.Army, (PathfindingFlags)0, null))
				{
					float propertyValue = aidata.Army.GetPropertyValue(SimulationProperties.Movement);
					if (propertyValue <= 0f)
					{
						if (this.IsArmyBesiegingACity(aidata2.Army.GUID))
						{
							continue;
						}
						propertyValue = aidata2.Army.GetPropertyValue(SimulationProperties.Movement);
						if (propertyValue > 0f && aicommanderMission_RegroupArmyAt.TransferUnits(aidata.GameEntity.GUID))
						{
							aicommanderMission_RegroupArmyAt.targetTransferArmy = aidata;
							aicommanderMission_RegroupArmyAt.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
							int num = this.UnitGuids.Length;
							Array.Resize<GameEntityGUID>(ref this.UnitGuids, this.UnitGuids.Length + aicommanderMission_RegroupArmyAt.UnitGuids.Length);
							for (int i = 0; i < aicommanderMission_RegroupArmyAt.UnitGuids.Length; i++)
							{
								this.UnitGuids[num + i] = aicommanderMission_RegroupArmyAt.UnitGuids[i];
							}
						}
					}
					else if (this.TransferUnits(aidata2.GameEntity.GUID))
					{
						this.targetTransferArmy = aidata2;
						base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
						int num2 = aicommanderMission_RegroupArmyAt.UnitGuids.Length;
						Array.Resize<GameEntityGUID>(ref aicommanderMission_RegroupArmyAt.UnitGuids, this.UnitGuids.Length + aicommanderMission_RegroupArmyAt.UnitGuids.Length);
						for (int j = 0; j < this.UnitGuids.Length; j++)
						{
							aicommanderMission_RegroupArmyAt.UnitGuids[num2 + j] = this.UnitGuids[j];
						}
					}
					return true;
				}
			}
		}
		return false;
	}

	private void ClampUnitToGarrison(IGarrison currentTarget, GameEntityGUID[] unitListCandidate, out GameEntityGUID[] unitsToGarrison)
	{
		int val = Math.Max(0, currentTarget.MaximumUnitSlot - currentTarget.CurrentUnitSlot);
		int num = Math.Min(unitListCandidate.Length, val);
		if (num <= 0)
		{
			unitsToGarrison = null;
		}
		else
		{
			unitsToGarrison = new GameEntityGUID[num];
			for (int i = 0; i < unitListCandidate.Length; i++)
			{
				if (i == num)
				{
					break;
				}
				unitsToGarrison[i] = unitListCandidate[i];
			}
		}
	}

	private bool IsArmyBesiegingACity(GameEntityGUID armyGUID)
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(armyGUID);
		if (aidata == null)
		{
			return false;
		}
		District district = this.worldPositionningService.GetDistrict(aidata.Army.WorldPosition);
		if (district == null)
		{
			return false;
		}
		City city = district.City;
		return city != null && (city.Empire != aidata.Army.Empire && city.BesiegingEmpire == aidata.Army.Empire);
	}

	private void MoveOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result != PostOrderResponse.Processed)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		this.orderExecuted = true;
		this.currentTicket = null;
	}

	private void OrderTransferUnitToArmyTicketRaised(object sender, TicketRaisedEventArgs e)
	{
		if (e.Result != PostOrderResponse.Processed)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		base.AIDataArmyGUID = GameEntityGUID.Zero;
		this.currentTicket = null;
	}

	private void OrderTransferUnitToNewArmyTicketRaised(object sender, TicketRaisedEventArgs e)
	{
		this.createFirstArmyTicket = null;
		if (e.Result != PostOrderResponse.Processed)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Initializing;
			return;
		}
		OrderTransferGarrisonToNewArmy orderTransferGarrisonToNewArmy = e.Order as OrderTransferGarrisonToNewArmy;
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(orderTransferGarrisonToNewArmy.ArmyGuid);
		if (aidata != null)
		{
			aidata.UnassignArmyMission();
			for (int i = 0; i < this.UnitGuids.Length; i++)
			{
				AIData_Unit aidata_Unit;
				this.aiDataRepository.TryGetAIData<AIData_Unit>(this.UnitGuids[i], out aidata_Unit);
				if (aidata_Unit == null)
				{
					Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] no AIData for Unit {0}", new object[]
					{
						this.UnitGuids[i].ToString()
					});
				}
				else
				{
					aidata_Unit.TryUnLockUnit(base.InternalGUID);
				}
			}
			base.AIDataArmyGUID = aidata.GameEntity.GUID;
			if (!aidata.AssignCommanderMission(this))
			{
				Diagnostics.LogWarning(string.Format("LOCKING Problem in [AICommanderMission_RegroupArmyAt]", new object[0]));
			}
		}
	}

	private void SetRequestMessageExecutionState(RequestUnitListMessage.RequestUnitListState executionState)
	{
		if (this.AICommanderRegroupArmies.RequestUnitListMessageID == GameEntityGUID.Zero)
		{
			return;
		}
		RequestUnitListMessage requestUnitListMessage = base.Commander.AIPlayer.Blackboard.GetMessage(this.AICommanderRegroupArmies.RequestUnitListMessageID) as RequestUnitListMessage;
		if (requestUnitListMessage != null)
		{
			requestUnitListMessage.ExecutionState = executionState;
		}
	}

	private bool TransferUnits(GameEntityGUID destinationGuid)
	{
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata.Army.IsInEncounter || aidata.Army.IsLocked)
		{
			Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Transfer source IsInEncounter or IsLocked");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(destinationGuid, out gameEntity))
		{
			Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Destination Guid is not valid");
			return false;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison == null)
		{
			Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Destination Guid is not a Garrison");
			return false;
		}
		if (garrison.IsInEncounter)
		{
			Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Transfer destination IsInEncounter");
			return false;
		}
		if (garrison is Army)
		{
			Army army = garrison as Army;
			if (army.IsLocked)
			{
				Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Transfer destination IsLocked");
				return false;
			}
			if (this.worldPositionningService.GetDistance(aidata.Army.WorldPosition, army.WorldPosition) != 1 || !this.pathfindingService.IsTransitionPassable(aidata.Army.WorldPosition, army.WorldPosition, aidata.Army, (PathfindingFlags)0, null))
			{
				Diagnostics.LogWarning("[AICommanderMission_RegroupArmyAt] Transfer destination is not valid");
				return false;
			}
		}
		GameEntityGUID[] array = null;
		this.ClampUnitToGarrison(garrison, this.UnitGuids, out array);
		if (array != null && array.Length != 0)
		{
			OrderTransferUnits order = new OrderTransferUnits(base.Commander.Empire.Index, base.AIDataArmyGUID, destinationGuid, array, false);
			base.Commander.Empire.PlayerControllers.Server.PostOrder(order, out this.currentTicket, new EventHandler<TicketRaisedEventArgs>(this.OrderTransferUnitToArmyTicketRaised));
			if (aidata.CommanderMission != this)
			{
				Diagnostics.LogWarning("LOCKING: [AICommanderMission_RegroupArmyAt] Strange desynchronization between the actual Army CommanderMission and the current CommanderMission");
			}
			aidata.UnassignCommanderMission();
			return true;
		}
		return false;
	}

	public GameEntityGUID SourceGuid = GameEntityGUID.Zero;

	public GameEntityGUID[] UnitGuids;

	private Ticket createFirstArmyTicket;

	private Ticket currentTicket;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private AIEntity_Empire majorEmpireAIEntity;

	private bool orderExecuted;

	private PathfindingContext pathfindingContext;

	private IPathfindingService pathfindingService;

	private AIData_Army targetTransferArmy;

	private IWorldPositionningService worldPositionningService;
}
