using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using UnityEngine;

public class TickableRepository : AIHelper, ITickableRepositoryAIHelper, IService
{
	public TickableRepository()
	{
		this.tickables = new List<ITickable>();
		this.updatables = new List<IUpdatable>();
		this.tickMaxTimer = 0.2f;
		this.tickOutGameMaxTimer = 0.05f;
		this.tickLimit = 10;
		this.tickLimitOutGame = 20;
	}

	public ReadOnlyCollection<ITickable> Tickables
	{
		get
		{
			return this.tickables.AsReadOnly();
		}
	}

	public override IEnumerator Initialize(IServiceContainer serviceContainer, Game game)
	{
		yield return base.Initialize(serviceContainer, game);
		this.encounterRepositoryService = game.Services.GetService<IEncounterRepositoryService>();
		serviceContainer.AddService<ITickableRepositoryAIHelper>(this);
		this.eventService = Services.GetService<IEventService>();
		if (this.eventService != null)
		{
			this.eventService.EventRaise += this.EventService_EventRaise;
		}
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.endTurnService.EndTurnRequested += this.EndTurnService_EndTurnRequested;
		this.playerControllerservice = game.Services.GetService<IPlayerControllerRepositoryService>();
		yield break;
	}

	public void Register(ITickable tickable)
	{
		if (tickable == null)
		{
			return;
		}
		List<ITickable> obj = this.tickables;
		lock (obj)
		{
			if (!this.tickables.Contains(tickable))
			{
				this.tickables.Add(tickable);
			}
		}
	}

	public override void Release()
	{
		base.Release();
		this.tickables.Clear();
		this.updatables.Clear();
		this.encounterRepositoryService = null;
		this.eventService.EventRaise -= this.EventService_EventRaise;
		this.eventService = null;
		this.endTurnService.EndTurnRequested -= this.EndTurnService_EndTurnRequested;
		this.endTurnService = null;
		this.playerControllerservice = null;
	}

	public override void RunAIThread()
	{
		base.RunAIThread();
		this.tickTimer = 0f;
		this.index = 0;
		List<ITickable> obj = this.tickables;
		lock (obj)
		{
			while (this.index < this.tickables.Count)
			{
				this.tickables[this.index].State = TickableState.NeedTick;
				this.index++;
			}
		}
		this.index = 0;
	}

	public void Tick(bool isInGameState = true)
	{
		DateTime now = DateTime.Now;
		if (this.updatables.Count > 0)
		{
			List<IUpdatable> obj = this.updatables;
			lock (obj)
			{
				if (!this.updatables[0].Update())
				{
					this.updatables.RemoveAt(0);
				}
			}
		}
		this.tickTimer += Time.deltaTime;
		if (isInGameState)
		{
			if (this.tickTimer < this.tickMaxTimer)
			{
				return;
			}
		}
		else if (this.tickTimer < this.tickOutGameMaxTimer)
		{
			return;
		}
		bool flag = false;
		using (IEnumerator<Encounter> enumerator = this.encounterRepositoryService.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				if (enumerator.Current.EncounterState != EncounterState.BattleHasEnded)
				{
					flag = true;
					break;
				}
			}
		}
		this.tickTimer = 0f;
		if (this.index >= this.tickables.Count)
		{
			this.index = 0;
		}
		List<ITickable> obj2 = this.tickables;
		lock (obj2)
		{
			int num = 0;
			while (this.index < this.tickables.Count)
			{
				if (this.tickables[this.index] != null)
				{
					if (this.tickables[this.index].State == TickableState.NeedTick || this.tickables[this.index].State == TickableState.Optional)
					{
						this.tickables[this.index].Tick();
						num++;
					}
					this.index++;
					if ((DateTime.Now - now).TotalMilliseconds > 75.0)
					{
						break;
					}
					if (isInGameState)
					{
						if (num > this.tickLimit)
						{
							break;
						}
						if (flag && num > this.tickLimitBattle)
						{
							break;
						}
					}
					if (num > this.tickLimitOutGame)
					{
						break;
					}
				}
				else
				{
					this.tickables.RemoveAt(this.index);
				}
			}
		}
	}

	public void Unregister(ITickable tickable)
	{
		List<ITickable> obj = this.tickables;
		lock (obj)
		{
			this.tickables.RemoveAll((ITickable match) => match == tickable);
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent is EventBeginTurn)
		{
			this.numberOfLivingEmpires = 0;
			int num = 0;
			while (num < base.Game.Empires.Length && base.Game.Empires[num] is MajorEmpire)
			{
				if (!(base.Game.Empires[num] as MajorEmpire).ELCPIsEliminated && base.Game.Empires[num].IsControlledByAI)
				{
					this.numberOfLivingEmpires++;
				}
				num++;
			}
			this.tickLimit = 9;
			this.tickLimitOutGame = 20;
			this.tickLimitBattle = Mathf.Max(0, this.numberOfLivingEmpires - 5) / 2;
			if (this.playerControllerservice.ActivePlayerController.Empire == null || (this.playerControllerservice.ActivePlayerController.Empire is MajorEmpire && (this.playerControllerservice.ActivePlayerController.Empire as MajorEmpire).IsEliminated))
			{
				this.tickLimit += 5 + this.numberOfLivingEmpires;
				this.tickLimitOutGame += this.numberOfLivingEmpires;
				this.lastEndTurnRequestTurn = base.Game.Turn;
			}
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP TickableRepository registered new turn {0}, {1} AI empires are still alive, setting tick limits to {2},{3} and {4}", new object[]
				{
					(e.RaisedEvent as EventBeginTurn).Turn,
					this.numberOfLivingEmpires,
					this.tickLimit,
					this.tickLimitOutGame,
					this.tickLimitBattle
				});
			}
		}
	}

	private void EndTurnService_EndTurnRequested(object sender, EventArgs e)
	{
		if (this.lastEndTurnRequestTurn == base.Game.Turn)
		{
			return;
		}
		this.tickLimit += 5 + this.numberOfLivingEmpires;
		this.tickLimitOutGame += this.numberOfLivingEmpires;
		this.lastEndTurnRequestTurn = base.Game.Turn;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP TickableRepository registered human end turn request, setting ticklimits to {0} and {1}", new object[]
			{
				this.tickLimit,
				this.tickLimitOutGame
			});
		}
	}

	public void RegisterUpdate(IUpdatable updatable)
	{
		if (updatable == null)
		{
			return;
		}
		List<IUpdatable> obj = this.updatables;
		lock (obj)
		{
			if (!this.updatables.Contains(updatable))
			{
				this.updatables.Add(updatable);
			}
		}
	}

	public void UnregisterUpdate(IUpdatable updatable)
	{
		List<IUpdatable> obj = this.updatables;
		lock (obj)
		{
			this.updatables.RemoveAll((IUpdatable match) => match == updatable);
		}
	}

	private int index;

	private List<ITickable> tickables;

	private float tickMaxTimer;

	private float tickOutGameMaxTimer;

	private float tickTimer;

	private IEncounterRepositoryService encounterRepositoryService;

	private int tickLimit;

	private int tickLimitOutGame;

	private IEventService eventService;

	private IEndTurnService endTurnService;

	private int numberOfLivingEmpires;

	private int lastEndTurnRequestTurn;

	private IPlayerControllerRepositoryService playerControllerservice;

	private int tickLimitBattle;

	private List<IUpdatable> updatables;
}
