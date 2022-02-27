using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Utilities.Maps;
using UnityEngine;

public class Encounter : IDisposable, IVisibilityProvider, IGameEntity, IWorldEntityMappingOverride
{
	public Encounter(GameEntityGUID encounterGUID)
	{
		Diagnostics.Assert(encounterGUID != GameEntityGUID.Zero, "Encounter GUID is null.");
		this.GUID = encounterGUID;
		this.Contenders = new List<Contender>();
		this.Empires = new List<global::Empire>();
		this.battleSequenceDatabase = Databases.GetDatabase<BattleSequence>(false);
		Diagnostics.Assert(this.battleSequenceDatabase != null);
		this.EventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.EventService != null);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.playerControllerRepositoryService = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(this.playerControllerRepositoryService != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.BattleZone = new BattleZone_Battle(this, this.worldPositionningService.World.WorldParameters);
		ISessionService service2 = Services.GetService<ISessionService>();
		Diagnostics.Assert(service2 != null);
		string lobbyData = service2.Session.GetLobbyData<string>("EncounterSequence", BattleEncounter.DefaultBattleSequenceName);
		BattleSequence battleSequence;
		if (this.battleSequenceDatabase.TryGetValue(lobbyData, out battleSequence))
		{
			this.BattleSequence = battleSequence;
		}
		else
		{
			Diagnostics.LogError("Can't found battle sequence named {0}.", new object[]
			{
				lobbyData
			});
		}
		this.encounterUnitSpawnEventHandler = new EventHandler<EncounterUnitSpawnEventArgs>(this.Contender_EncounterUnitSpawn);
		this.ExternalArmies = new List<GameEntityGUID>();
	}

	public event EventHandler<BattleActionStateChangeEventArgs> BattleActionStateChange;

	public event EventHandler<ContenderCollectionChangeEventArgs> ContenderCollectionChange;

	public event EventHandler<ContenderDeploymentChangeEventArgs> ContenderDeploymentChange;

	public event EventHandler<ContenderStateChangeEventArgs> ContenderStateChange;

	public event EventHandler<ContenderUpdateEventArgs> ContenderUpdate;

	public event EventHandler<EncounterStateChangeEventArgs> EncounterStateChange;

	public event EventHandler<EncounterUnitSpawnEventArgs> EncounterUnitSpawn;

	public event EventHandler<RoundUpdateEventArgs> RoundUpdate;

	public event EventHandler RoundEnd;

	public event EventHandler<TargetingPhaseStartEventArgs> TargetingPhaseUpdate;

	public event EventHandler<RoundUpdateEventArgs> DeploymentUpdate;

	public event EventHandler<DeployementPhaseTimeChangedEventArgs> DeployementPhaseTimeChanged;

	public event EventHandler EncounterDisposed;

	public IEnumerable<Contender> GetAlliedContendersFromContender(Contender contender)
	{
		if (this.Contenders != null)
		{
			return from match in this.Contenders
			where match.Group == contender.Group
			select match;
		}
		return null;
	}

	public IEnumerable<Contender> GetAlliedContendersFromEmpire(global::Empire empire)
	{
		Contender firstAlliedContenderFromEmpire = this.GetFirstAlliedContenderFromEmpire(empire);
		if (firstAlliedContenderFromEmpire != null)
		{
			return this.GetAlliedContendersFromContender(firstAlliedContenderFromEmpire);
		}
		return null;
	}

	public IEnumerable<Contender> GetAlliedContenderFromGroup(byte group)
	{
		if (this.Contenders != null)
		{
			return from match in this.Contenders
			where match.Group == @group
			select match;
		}
		return null;
	}

	public Contender GetFirstAlliedContenderFromEmpire(global::Empire empire)
	{
		if (this.Contenders != null)
		{
			return this.Contenders.FirstOrDefault((Contender match) => match.Empire.Index == empire.Index);
		}
		return null;
	}

	public IEnumerable<Contender> GetEnemiesContenderFromContender(Contender contender)
	{
		if (this.Contenders != null)
		{
			return from match in this.Contenders
			where match.Group != contender.Group
			select match;
		}
		return null;
	}

	public IEnumerable<Contender> GetEnemiesContenderFromEmpire(global::Empire empire)
	{
		Contender firstEnemyContenderFromEmpire = this.GetFirstEnemyContenderFromEmpire(empire);
		if (firstEnemyContenderFromEmpire != null)
		{
			return this.GetAlliedContenderFromGroup(firstEnemyContenderFromEmpire.Group);
		}
		return null;
	}

	public IEnumerable<Contender> GetEnemiesContenderFromGroup(byte group)
	{
		if (this.Contenders != null)
		{
			return from match in this.Contenders
			where match.Group != @group
			select match;
		}
		return null;
	}

	public IEnumerable<Contender> GetOwnContenderFromEmpire(global::Empire empire)
	{
		if (this.Contenders != null)
		{
			global::Empire playerEmpire = this.playerControllerRepositoryService.ActivePlayerController.Empire as global::Empire;
			return from match in this.Contenders
			where match.Empire == playerEmpire
			select match;
		}
		return null;
	}

	public Contender GetMainEnemyContenderFromContender(Contender contender)
	{
		if (this.Contenders != null)
		{
			for (int i = 0; i < this.Contenders.Count; i++)
			{
				if (this.Contenders[i].Group != contender.Group && this.Contenders[i].IsMainContender)
				{
					return this.Contenders[i];
				}
			}
		}
		return null;
	}

	public Contender GetEnemyContenderWithAbilityFromContender(Contender contender, StaticString unitAbility)
	{
		Contender contender2 = null;
		if (this.Contenders != null)
		{
			for (int i = 0; i < this.Contenders.Count; i++)
			{
				if (this.Contenders[i].Group != contender.Group)
				{
					if (contender2 != null && this.Contenders[i].IsMainContender)
					{
						contender2 = this.Contenders[i];
					}
					for (int j = 0; j < this.Contenders[i].EncounterUnits.Count; j++)
					{
						if (this.Contenders[i].EncounterUnits[j].Unit.CheckUnitAbility(unitAbility, -1))
						{
							return this.Contenders[i];
						}
					}
				}
			}
		}
		return contender2;
	}

	public Contender GetFirstEnemyContenderFromEmpire(global::Empire empire)
	{
		Encounter.<GetFirstEnemyContenderFromEmpire>c__AnonStorey894 <GetFirstEnemyContenderFromEmpire>c__AnonStorey = new Encounter.<GetFirstEnemyContenderFromEmpire>c__AnonStorey894();
		<GetFirstEnemyContenderFromEmpire>c__AnonStorey.empire = empire;
		if (this.Contenders == null)
		{
			return null;
		}
		Contender empireContender = this.Contenders.FirstOrDefault((Contender match) => match.Empire.Index == <GetFirstEnemyContenderFromEmpire>c__AnonStorey.empire.Index);
		if (empireContender == null)
		{
			return null;
		}
		return this.Contenders.FirstOrDefault((Contender match) => match.Group != empireContender.Group);
	}

	public EncounterResult GetEncounterResultForEmpire(global::Empire empire)
	{
		if (this.encounterState != EncounterState.BattleHasEnded)
		{
			return EncounterResult.BattleIsStillInProgress;
		}
		IEnumerable<Contender> enumerable = from contender in this.GetAlliedContendersFromEmpire(empire)
		where contender.IsTakingPartInBattle
		select contender;
		bool flag = true;
		if (enumerable == null)
		{
			return EncounterResult.NotAContender;
		}
		foreach (Contender contender3 in enumerable)
		{
			if (contender3.ContenderState == ContenderState.Survived)
			{
				flag = false;
				break;
			}
		}
		IEnumerable<Contender> enumerable2 = from contender in this.GetEnemiesContenderFromEmpire(empire)
		where contender.IsTakingPartInBattle
		select contender;
		bool flag2 = true;
		foreach (Contender contender2 in enumerable2)
		{
			if (contender2.ContenderState == ContenderState.Survived)
			{
				flag2 = false;
				break;
			}
		}
		if ((flag && flag2) || (!flag && !flag2))
		{
			return EncounterResult.Draw;
		}
		if (flag2)
		{
			return EncounterResult.Victory;
		}
		return EncounterResult.Defeat;
	}

	public EncounterUnit GetEncounterUnitByGUID(GameEntityGUID unitGUID)
	{
		if (this.Contenders != null)
		{
			for (int i = 0; i < this.Contenders.Count; i++)
			{
				Contender contender = this.Contenders[i];
				for (int j = 0; j < contender.EncounterUnits.Count; j++)
				{
					if (contender.EncounterUnits[j].Unit.GUID == unitGUID)
					{
						return contender.EncounterUnits[j];
					}
				}
			}
		}
		return null;
	}

	public bool IsInDeploymentPhase()
	{
		return this.IsInPhase(ContenderState.Deployment) || this.IsInPhase(ContenderState.ReadyForBattle);
	}

	public bool IsInTargetingPhase()
	{
		return this.IsInPhase(ContenderState.TargetingPhaseInProgress) || this.IsInPhase(ContenderState.ReadyForNextPhase);
	}

	public bool IsInResolutionPhase()
	{
		return this.IsInPhase(ContenderState.RoundInProgress) || this.IsInPhase(ContenderState.ReadyForNextRound);
	}

	public bool IsInPhase(ContenderState contenderState)
	{
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			if (this.Contenders[i].ContenderState == contenderState)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsPlayerInPhase(ContenderState contenderState)
	{
		global::Empire empire = this.playerControllerRepositoryService.ActivePlayerController.Empire as global::Empire;
		foreach (Contender contender in this.GetOwnContenderFromEmpire(empire))
		{
			if (contender.ContenderState == contenderState)
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerable<ILineOfSightEntity> LineOfSightEntities
	{
		get
		{
			yield break;
		}
	}

	public bool VisibilityDirty { get; set; }

	public IEnumerable<global::Empire> VisibilityEmpires
	{
		get
		{
			if (this.Contenders != null)
			{
				for (int index = 0; index < this.Contenders.Count; index++)
				{
					if (this.Contenders[index].Empire != null)
					{
						yield return this.Contenders[index].Empire;
					}
				}
			}
			yield break;
		}
	}

	public IEnumerable<IVisibilityArea> VisibleAreas
	{
		get
		{
			if (this.encounterState == EncounterState.BattleHasEnded || this.encounterState == EncounterState.Invalid)
			{
				yield break;
			}
			if (this.Contenders == null)
			{
				yield break;
			}
			for (int index = 0; index < this.Contenders.Count; index++)
			{
				yield return this.Contenders[index];
			}
			yield break;
		}
	}

	~Encounter()
	{
		this.Dispose(false);
	}

	public BattlePhase BattlePhase { get; private set; }

	public float BattleRoundDuration
	{
		get
		{
			if (this.BattlePhase == null)
			{
				return 0f;
			}
			return this.BattlePhase.RoundDuration;
		}
	}

	public int BattleRoundIndex { get; private set; }

	public int BattlePhaseIndex { get; private set; }

	public BattleSequence BattleSequence { get; private set; }

	public List<Contender> Contenders { get; private set; }

	public RoundReport CurrentRoundReport { get; private set; }

	public List<GameEntityGUID> ExternalArmies { get; private set; }

	public List<global::Empire> Empires { get; private set; }

	public EncounterState EncounterState
	{
		get
		{
			return this.encounterState;
		}
		set
		{
			if (this.encounterState != value)
			{
				this.encounterState = value;
				EncounterStateChangeEventArgs e = new EncounterStateChangeEventArgs(this, value);
				this.OnEncounterStateChange(e);
			}
		}
	}

	public GameEntityGUID GUID { get; private set; }

	public List<GuiGarrison> ManualSortedReinforcements { get; set; }

	public OrderCreateEncounter OrderCreateEncounter { get; set; }

	public OrderBeginEncounter OrderBeginEncounter { get; set; }

	public BattleZone_Battle BattleZone { get; private set; }

	public Encounter.PhaseTime SetupPhaseTime { get; set; }

	public Encounter.PhaseTime DeployementPhaseTime { get; set; }

	public bool Retreat { get; private set; }

	public Encounter.PhaseTime TargetingPhaseTime { get; set; }

	private IEventService EventService { get; set; }

	public bool Instant { get; set; }

	public void NotifyDeployementPhaseTimeChanged()
	{
		if (this.DeployementPhaseTimeChanged != null)
		{
			this.DeployementPhaseTimeChanged(this, new DeployementPhaseTimeChangedEventArgs(this.DeployementPhaseTime));
		}
	}

	public bool ChangeDeployment(GameEntityGUID contenderGuid, Deployment deployment)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			contender.Deployment = deployment;
			this.OnContenderDeploymentChange(new ContenderDeploymentChangeEventArgs(contender, deployment));
			return true;
		}
		return false;
	}

	public bool ChangeUnitDeployment(GameEntityGUID contenderGuid, GameEntityGUID unitGuid, WorldPosition worldPosition)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			for (int i = 0; i < contender.Deployment.UnitDeployment.Length; i++)
			{
				if (contender.Deployment.UnitDeployment[i].UnitGUID == unitGuid)
				{
					contender.Deployment.UnitDeployment[i].WorldPosition = worldPosition;
					this.OnContenderDeploymentChange(new ContenderDeploymentChangeEventArgs(contender, contender.Deployment));
					return true;
				}
			}
		}
		return false;
	}

	public void Dispose()
	{
		this.Dispose(true);
	}

	public void ForwardRoundEnd()
	{
		this.OnRoundEnd(new EventArgs());
	}

	public void ForwardRoundUpdate(RoundReport roundReport, bool immediateParsing)
	{
		this.OnRoundUpdate(new RoundUpdateEventArgs(this, roundReport, immediateParsing));
	}

	public void ForwardTargetingPhaseUpdate(RoundReport roundReport, double endTime)
	{
		this.OnTargetingPhaseUpdate(new TargetingPhaseStartEventArgs(this, roundReport, endTime));
	}

	public void ForwardDeploymentUpdate(RoundReport roundReport)
	{
		this.OnDeploymentUpdate(new RoundUpdateEventArgs(this, roundReport, true));
	}

	public List<IDroppable> GetRewardFor(int empireIndex)
	{
		if (this.rewardByEmpireIndex.ContainsKey(empireIndex))
		{
			return this.rewardByEmpireIndex[empireIndex];
		}
		return new List<IDroppable>();
	}

	public void IncludeContenderInEncounter(GameEntityGUID contenderGuid, bool include)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender == null)
		{
			return;
		}
		contender.IsTakingPartInBattle = include;
		this.OnContenderUpdate(new ContenderUpdateEventArgs(contender));
	}

	public bool IsGarrisonInEncounter(GameEntityGUID guid, bool checkEvenIfFinished = false)
	{
		return (checkEvenIfFinished || this.EncounterState != EncounterState.BattleHasEnded) && ((this.Contenders != null && this.Contenders.Exists((Contender match) => match.GUID == guid)) || this.OrderCreateEncounter.ContenderGUIDs.Any((GameEntityGUID match) => match == guid));
	}

	public void JoinAsSpectator(GameEntityGUID armyGuid, bool join)
	{
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(armyGuid, out gameEntity))
		{
			ILockableTarget lockableTarget = gameEntity as ILockableTarget;
			if (lockableTarget.IsLocked != join)
			{
				if (lockableTarget != null)
				{
					lockableTarget.Lock(this.GUID, join);
				}
				IGarrison garrison = gameEntity as IGarrison;
				if (garrison != null)
				{
					if (join)
					{
						garrison.JoinEncounterAsSpectator(this);
						this.ExternalArmies.Add(armyGuid);
					}
					else
					{
						garrison.LeaveEncounterAsSpectator(this);
					}
				}
			}
		}
	}

	public bool Join(GameEntityGUID contenderGuid, bool isCity, bool isCamp, bool isVillage, byte group, bool isReinforcement, Deployment deployment, int reinforcementRanking, out Contender contender)
	{
		contender = null;
		if (this.Contenders.Any((Contender iterator) => iterator.GUID == contenderGuid))
		{
			Diagnostics.LogError("Contender (guid: {0}) has already joinded the encounter.", new object[]
			{
				contenderGuid
			});
			return false;
		}
		if (!isReinforcement)
		{
			for (int i = 0; i < this.Contenders.Count; i++)
			{
				if (this.Contenders[i].IsMainContender && this.Contenders[i].Garrison.UnitsCount == 0)
				{
					this.Contenders[i].IsMainContender = false;
				}
			}
		}
		if (isCity)
		{
			contender = new Contender_City(contenderGuid, group, isReinforcement, this);
		}
		else if (isCamp)
		{
			contender = new Contender_Camp(contenderGuid, group, isReinforcement, this);
		}
		else if (isVillage)
		{
			contender = new Contender_Village(contenderGuid, group, isReinforcement, this);
		}
		else
		{
			contender = new Contender(contenderGuid, group, isReinforcement, this);
		}
		contender.ReinforcementRanking = reinforcementRanking;
		this.Join(contender, deployment);
		this.Contenders.ForEach(delegate(Contender iterator)
		{
			iterator.VisibilityDirty = true;
		});
		return true;
	}

	public void RegisterRewards(int empireIndex, IDroppable reward)
	{
		if (!this.rewardByEmpireIndex.ContainsKey(empireIndex))
		{
			this.rewardByEmpireIndex.Add(empireIndex, new List<IDroppable>());
		}
		this.rewardByEmpireIndex[empireIndex].Add(reward);
	}

	public void SetTargetingPhaseInProgress()
	{
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			this.SetContenderState(this.Contenders[i].GUID, ContenderState.TargetingPhaseInProgress);
		}
	}

	public void SetBattleInProgress(bool setContenders = true)
	{
		this.EncounterState = EncounterState.BattleIsInProgress;
		if (setContenders)
		{
			for (int i = 0; i < this.Contenders.Count; i++)
			{
				this.SetContenderState(this.Contenders[i].GUID, ContenderState.RoundInProgress);
			}
		}
	}

	public void SetContenderIsRetreating(GameEntityGUID contenderGuid, bool isRetreating)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			if (isRetreating && contender.Garrison != null && !contender.HasEnoughActionPoint(1f))
			{
				isRetreating = false;
			}
			contender.IsRetreating = isRetreating;
			this.Retreat = (this.Retreat || isRetreating);
		}
	}

	public bool SetContenderState(GameEntityGUID contenderGuid, ContenderState contenderState)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			if (contender.ContenderState == ContenderState.Invalid)
			{
				Diagnostics.LogError("Invalid contender state '{0}'; now changing to '{1}'.", new object[]
				{
					contender.ContenderState,
					contenderState
				});
			}
			if (contender.ContenderState != contenderState)
			{
				if (contenderState == ContenderState.ReadyForBattle)
				{
					contender.SetReadyForBattle();
				}
				contender.ContenderState = contenderState;
				this.OnContenderStateChange(new ContenderStateChangeEventArgs(contender, contenderState));
			}
			return true;
		}
		return false;
	}

	public void SetContenderOptionChoice(GameEntityGUID contenderGuid, EncounterOptionChoice contenderEncounterOptionChoice)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			contender.ContenderEncounterOptionChoice = contenderEncounterOptionChoice;
		}
		else
		{
			Diagnostics.LogWarning("SetContenderOptionChoice: contender with guid = '{0}' not found", new object[]
			{
				contenderGuid
			});
		}
	}

	public bool SetReadyForBattle(GameEntityGUID contenderGuid)
	{
		return this.SetContenderState(contenderGuid, ContenderState.ReadyForBattle);
	}

	public bool SetReadyForDeployment(GameEntityGUID contenderGuid)
	{
		return this.SetContenderState(contenderGuid, ContenderState.ReadyForDeployment);
	}

	public bool SetReadyForNextPhase(GameEntityGUID contenderGuid)
	{
		return this.SetContenderState(contenderGuid, ContenderState.ReadyForNextPhase);
	}

	public bool SetReadyForNextRound(GameEntityGUID contenderGuid)
	{
		return this.SetContenderState(contenderGuid, ContenderState.ReadyForNextRound);
	}

	public bool SetDeploymentFinished(GameEntityGUID contenderGuid)
	{
		return this.SetContenderState(contenderGuid, ContenderState.ReadyForBattle_DeploymentFinished);
	}

	public bool SwapUnitDeployment(GameEntityGUID contenderGuid, GameEntityGUID unit1Guid, WorldPosition finalUnit1Position, GameEntityGUID unit2Guid, WorldPosition finalUnit2Position)
	{
		Contender contender = this.Contenders.Find((Contender iterator) => iterator.GUID == contenderGuid);
		if (contender != null)
		{
			int num = 0;
			for (int i = 0; i < contender.Deployment.UnitDeployment.Length; i++)
			{
				if (contender.Deployment.UnitDeployment[i].UnitGUID == unit1Guid)
				{
					contender.Deployment.UnitDeployment[i].WorldPosition = finalUnit1Position;
					num++;
				}
				else if (contender.Deployment.UnitDeployment[i].UnitGUID == unit2Guid)
				{
					contender.Deployment.UnitDeployment[i].WorldPosition = finalUnit2Position;
					num++;
				}
			}
			Diagnostics.Assert(num == 2);
			this.OnContenderDeploymentChange(new ContenderDeploymentChangeEventArgs(contender, contender.Deployment));
		}
		return false;
	}

	public void SwitchContendersReinforcementRanking(GameEntityGUID firstContenderGuid, GameEntityGUID secondContenderGuid)
	{
		Contender contender = null;
		Contender contender2 = null;
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			if (this.Contenders[i].GUID == firstContenderGuid)
			{
				contender = this.Contenders[i];
			}
			else if (this.Contenders[i].GUID == secondContenderGuid)
			{
				contender2 = this.Contenders[i];
			}
		}
		if (contender != null && contender2 != null)
		{
			int reinforcementRanking = contender.ReinforcementRanking;
			contender.ReinforcementRanking = contender2.ReinforcementRanking;
			contender2.ReinforcementRanking = reinforcementRanking;
		}
	}

	public void ChangeContenderReinforcementRanking(GameEntityGUID contenderGuid, int newRanking)
	{
		Contender contender = null;
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			if (this.Contenders[i].GUID == contenderGuid)
			{
				contender = this.Contenders[i];
				break;
			}
		}
		if (contender.IsMainContender)
		{
			return;
		}
		if (contender.ReinforcementRanking == newRanking)
		{
			return;
		}
		int num = 1;
		for (int j = 0; j < this.Contenders.Count; j++)
		{
			if (this.Contenders[j].Group == contender.Group)
			{
				if (!this.Contenders[j].IsMainContender && this.Contenders[j] != contender)
				{
					num++;
					if (this.Contenders[j].ReinforcementRanking >= newRanking)
					{
						this.Contenders[j].ReinforcementRanking++;
					}
					else if (this.Contenders[j].ReinforcementRanking > contender.ReinforcementRanking)
					{
						this.Contenders[j].ReinforcementRanking--;
					}
				}
			}
		}
		contender.ReinforcementRanking = Mathf.Min(num, newRanking);
	}

	public bool TryResolve(out string mappingName)
	{
		mappingName = "Encounter";
		return true;
	}

	public bool TryResolve(out InterpreterContext context)
	{
		context = null;
		return true;
	}

	public void LogLastSnapShot()
	{
	}

	protected void Contender_EncounterUnitSpawn(object sender, EncounterUnitSpawnEventArgs args)
	{
		if (this.EncounterUnitSpawn != null)
		{
			this.EncounterUnitSpawn(this, args);
		}
	}

	protected virtual void Dispose(bool disposing)
	{
		if (!this.disposed)
		{
			if (disposing)
			{
				if (this.Contenders != null)
				{
					foreach (Contender contender in this.Contenders)
					{
						contender.Dispose();
					}
				}
				this.Contenders.Clear();
				GridMap<Encounter> gridMap = this.worldPositionningService.World.Atlas.GetMap(WorldAtlas.Maps.Encounters) as GridMap<Encounter>;
				Diagnostics.Assert(gridMap != null);
				foreach (WorldPosition worldPosition in this.BattleZone.GetWorldPositions())
				{
					gridMap.SetValue(worldPosition, null);
				}
			}
			this.Contenders = null;
			this.ExternalArmies.Clear();
			if (this.EncounterDisposed != null)
			{
				this.EncounterDisposed(this, null);
			}
		}
		this.disposed = true;
	}

	protected virtual void OnContenderCollectionChange(ContenderCollectionChangeEventArgs e)
	{
		if (this.ContenderCollectionChange != null)
		{
			this.ContenderCollectionChange(this, e);
		}
	}

	protected virtual void OnContenderDeploymentChange(ContenderDeploymentChangeEventArgs e)
	{
		if (this.ContenderDeploymentChange != null)
		{
			this.ContenderDeploymentChange(this, e);
		}
	}

	protected virtual void OnContenderStateChange(ContenderStateChangeEventArgs e)
	{
		if (this.ContenderStateChange != null)
		{
			this.ContenderStateChange(this, e);
		}
	}

	protected virtual void OnContenderUpdate(ContenderUpdateEventArgs e)
	{
		if (this.ContenderUpdate != null)
		{
			this.ContenderUpdate(this, e);
		}
	}

	protected virtual void OnEncounterStateChange(EncounterStateChangeEventArgs e)
	{
		bool flag = false;
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			Contender contender2 = this.Contenders[i];
			for (int j = 0; j < contender2.EncounterUnits.Count; j++)
			{
				if (contender2.EncounterUnits[j].Unit.SimulationObject == null)
				{
					flag = true;
					break;
				}
			}
		}
		if (!flag)
		{
			for (int k = 0; k < this.Contenders.Count; k++)
			{
				this.Contenders[k].TakeSnapShot(this);
			}
		}
		if (this.EncounterStateChange != null)
		{
			this.EncounterStateChange(this, e);
		}
		IEnumerable<global::Empire> enumerable = (from contender in this.Contenders
		select contender.Empire).Distinct<global::Empire>();
		foreach (global::Empire empire in enumerable)
		{
			this.EventService.Notify(new EventEncounterStateChange(empire, e));
		}
	}

	protected virtual void OnRoundEnd(EventArgs e)
	{
		if (this.CurrentRoundReport != null)
		{
			this.AnalyseRoundReport(this.CurrentRoundReport);
			this.CurrentRoundReport = null;
		}
		if (this.RoundEnd != null)
		{
			this.RoundEnd(this, e);
		}
	}

	protected virtual void OnRoundUpdate(RoundUpdateEventArgs e)
	{
		this.AnalyseRoundReportHeader(e.RoundReport);
		if (!e.ImmediateParsing)
		{
			this.CurrentRoundReport = e.RoundReport;
		}
		else
		{
			this.AnalyseRoundReport(e.RoundReport);
			this.CurrentRoundReport = null;
		}
		for (int i = 0; i < this.Contenders.Count; i++)
		{
			this.Contenders[i].OnRoundUpdate();
		}
		if (this.RoundUpdate != null)
		{
			this.RoundUpdate(this, e);
		}
	}

	protected virtual void OnDeploymentUpdate(RoundUpdateEventArgs e)
	{
		this.AnalyseRoundReportHeader(e.RoundReport);
		if (this.DeploymentUpdate != null)
		{
			this.DeploymentUpdate(this, e);
		}
	}

	protected virtual void OnTargetingPhaseUpdate(TargetingPhaseStartEventArgs e)
	{
		this.AnalyseRoundReportHeader(e.Report);
		if (this.TargetingPhaseUpdate != null)
		{
			this.TargetingPhaseUpdate(this, e);
		}
	}

	protected virtual void Join(Contender contender, Deployment deployment)
	{
		contender.Deployment = deployment;
		contender.ContenderState = ContenderState.Setup;
		this.Contenders.Add(contender);
		contender.BattleActionStateChange += this.Contender_BattleActionStateChange;
		contender.EncounterUnitSpawn += this.encounterUnitSpawnEventHandler;
		contender.TakeSnapShot(this);
		if (!this.Empires.Contains(contender.Empire))
		{
			this.Empires.Add(contender.Empire);
		}
		PathfindingContextMode pathfindingContextMode = PathfindingContextMode.Encounter;
		if (this.Contenders[0].Garrison is Army && ((Army)this.Contenders[0].Garrison).IsNaval)
		{
			pathfindingContextMode = PathfindingContextMode.NavalEncounter;
		}
		for (int i = 0; i < contender.EncounterUnits.Count; i++)
		{
			contender.EncounterUnits[i].Unit.PathfindingContextMode = pathfindingContextMode;
		}
		this.BattleZone.AddContenderDeployment(deployment);
		GridMap<Encounter> gridMap = this.worldPositionningService.World.Atlas.GetMap(WorldAtlas.Maps.Encounters) as GridMap<Encounter>;
		Diagnostics.Assert(gridMap != null);
		foreach (WorldPosition worldPosition in deployment.BattleZone.GetWorldPositions())
		{
			gridMap.SetValue(worldPosition, this);
		}
		this.OnContenderCollectionChange(new ContenderCollectionChangeEventArgs(ContenderCollectionChangeAction.Add, this, contender));
		this.OnContenderStateChange(new ContenderStateChangeEventArgs(contender, ContenderState.Setup));
	}

	private void AnalyseRoundReportHeader(RoundReport roundReport)
	{
		if (roundReport == null)
		{
			throw new ArgumentNullException("roundReport");
		}
		if (this.BattleSequence != null)
		{
			this.BattlePhaseIndex = roundReport.PhaseIndex;
			if (roundReport.PhaseIndex >= 0 && roundReport.PhaseIndex < this.BattleSequence.BattlePhases.Length)
			{
				this.BattlePhase = this.BattleSequence.BattlePhases[roundReport.PhaseIndex];
				Diagnostics.Assert(this.BattlePhase != null);
				Diagnostics.Assert(roundReport.RoundIndex < this.BattlePhase.NumberOfRounds);
			}
		}
		this.BattleRoundIndex = roundReport.RoundIndex;
	}

	private void AnalyseRoundReport(RoundReport roundReport)
	{
		Diagnostics.Assert(this.Contenders != null);
		Diagnostics.Assert(roundReport.RoundContenderReports != null);
		for (int i = 0; i < roundReport.RoundContenderReports.Count; i++)
		{
			RoundContenderReport roundContenderReport = roundReport.RoundContenderReports[i];
			Diagnostics.Assert(roundContenderReport != null);
			Contender contender = this.Contenders.Find((Contender encounterContender) => encounterContender.GUID == roundContenderReport.ContenderGUID);
			if (contender == null)
			{
				Diagnostics.LogError("Can't found contender instance in the encounter ({0:X8}). The simulation will not be replicated on the empire.", new object[]
				{
					roundContenderReport.ContenderGUID
				});
			}
			else
			{
				contender.AnalyseRoundReport(roundReport.RoundIndex, roundContenderReport);
			}
		}
	}

	private void Contender_BattleActionStateChange(object sender, BattleActionStateChangeEventArgs e)
	{
		if (this.BattleActionStateChange != null)
		{
			this.BattleActionStateChange(this, e);
		}
	}

	protected EventHandler<EncounterUnitSpawnEventArgs> encounterUnitSpawnEventHandler;

	private readonly IDatabase<BattleSequence> battleSequenceDatabase;

	private readonly IPlayerControllerRepositoryService playerControllerRepositoryService;

	private readonly IGameEntityRepositoryService gameEntityRepositoryService;

	private readonly IWorldPositionningService worldPositionningService;

	private bool disposed;

	private EncounterState encounterState;

	private Dictionary<int, List<IDroppable>> rewardByEmpireIndex = new Dictionary<int, List<IDroppable>>();

	public struct PhaseTime
	{
		public PhaseTime(double endTime, double duration)
		{
			this.EndTime = endTime;
			this.Duration = duration;
		}

		public double EndTime;

		public double Duration;
	}
}
