using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude.Unity.Framework;
using UnityEngine;

public class TickableRepository : AIHelper, ITickableRepositoryAIHelper, IService
{
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
		this.encounterRepositoryService = null;
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
		List<ITickable> obj = this.tickables;
		lock (obj)
		{
			DateTime now = DateTime.Now;
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
					if ((DateTime.Now - now).TotalMilliseconds > 150.0)
					{
						break;
					}
					if (isInGameState)
					{
						if (num > 9)
						{
							break;
						}
						if (flag && num > 0)
						{
							break;
						}
					}
					if (num > 20)
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

	private int index;

	private List<ITickable> tickables = new List<ITickable>();

	private float tickMaxTimer = 0.2f;

	private float tickOutGameMaxTimer = 0.05f;

	private float tickTimer;

	private IEncounterRepositoryService encounterRepositoryService;
}
