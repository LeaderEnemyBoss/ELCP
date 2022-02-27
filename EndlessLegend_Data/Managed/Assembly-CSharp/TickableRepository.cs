using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using Amplitude;
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
					if (this.tickables[this.index].State == TickableState.NeedTick)
					{
						goto IL_F4;
					}
					if (this.tickables[this.index].State == TickableState.Optional)
					{
						goto Block_9;
					}
					IL_17F:
					if ((DateTime.Now - now).TotalMilliseconds > 200.0)
					{
						break;
					}
					if (isInGameState && num > 10)
					{
						break;
					}
					if (num > 20)
					{
						break;
					}
					goto IL_1C6;
					Block_9:
					try
					{
						IL_F4:
						this.tickables[this.index].Tick();
					}
					catch (Exception ex)
					{
						if (this.tickables[this.index] != null)
						{
							Diagnostics.LogError("Exception while ticking the AI ({0}) : {1}", new object[]
							{
								this.tickables[this.index].GetType(),
								ex.ToString()
							});
						}
						else
						{
							Diagnostics.LogError("Exception while ticking the AI: {0}", new object[]
							{
								ex.ToString()
							});
						}
					}
					num++;
					goto IL_17F;
				}
				this.tickables.RemoveAt(this.index);
				this.index--;
				IL_1C6:
				this.index++;
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
}
