using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;

[Diagnostics.TagAttribute("AI")]
public class SynchronousJobRepository : AIHelper, ISynchronousJobRepositoryAIHelper, IService, ITickable
{
	public SynchronousJobRepository()
	{
		this.synchronousDelegates = new List<SynchronousJob>();
		this.tickLimit = 5;
	}

	public bool IsEmpty
	{
		get
		{
			return this.synchronousDelegates.Count == 0;
		}
	}

	public TickableState State { get; set; }

	public override IEnumerator Initialize(IServiceContainer serviceContainer, Game game)
	{
		yield return base.Initialize(serviceContainer, game);
		yield return base.BindService<ITickableRepositoryAIHelper>(serviceContainer, delegate(ITickableRepositoryAIHelper service)
		{
			this.tickableRepositoryAIHelper = service;
		});
		this.tickableRepositoryAIHelper.Register(this);
		serviceContainer.AddService<ISynchronousJobRepositoryAIHelper>(this);
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

	public void RegisterSynchronousJob(SynchronousJob job)
	{
		if (job == null)
		{
			throw new ArgumentNullException("job");
		}
		if (this.State != TickableState.NeedTick)
		{
			this.State = TickableState.NeedTick;
		}
		Diagnostics.Assert(this.synchronousDelegates != null);
		this.synchronousDelegates.Add(job);
	}

	public void Tick()
	{
		if (this.synchronousDelegates.Count == 0)
		{
			this.State = TickableState.NoTick;
			return;
		}
		if (this.lastJobIndex <= 0)
		{
			this.lastJobIndex = this.synchronousDelegates.Count - 1;
		}
		Diagnostics.Assert(this.synchronousDelegates != null);
		int num = 0;
		while (this.lastJobIndex >= 0 && num++ <= this.tickLimit)
		{
			SynchronousJob synchronousJob = this.synchronousDelegates[this.lastJobIndex];
			if (synchronousJob == null)
			{
				goto IL_83;
			}
			switch (synchronousJob())
			{
			case SynchronousJobState.Running:
				break;
			case SynchronousJobState.Success:
			case SynchronousJobState.Failure:
				goto IL_83;
			default:
				goto IL_83;
			}
			IL_73:
			this.lastJobIndex--;
			continue;
			IL_83:
			this.synchronousDelegates.RemoveAt(this.lastJobIndex);
			goto IL_73;
		}
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (e.RaisedEvent is EventBeginTurn)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("SynchronousJobRepository Registered Turn start {0} {1}", new object[]
				{
					base.Game != null,
					this.playerControllerservice != null
				});
			}
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
			this.tickLimit = 5;
			if (this.playerControllerservice.ActivePlayerController.Empire == null || (this.playerControllerservice.ActivePlayerController.Empire is MajorEmpire && (this.playerControllerservice.ActivePlayerController.Empire as MajorEmpire).IsEliminated))
			{
				this.tickLimit += 5 + this.numberOfLivingEmpires;
			}
		}
	}

	private void EndTurnService_EndTurnRequested(object sender, EventArgs e)
	{
		if (this.lastEndTurnRequestTurn == base.Game.Turn)
		{
			return;
		}
		this.tickLimit = 5 + this.numberOfLivingEmpires;
		this.lastEndTurnRequestTurn = base.Game.Turn;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("SynchronousJobRepository registered end turn request, setting ticklimit to {0}", new object[]
			{
				this.tickLimit
			});
		}
	}

	public override void Release()
	{
		base.Release();
		this.eventService.EventRaise -= this.EventService_EventRaise;
		this.eventService = null;
		this.endTurnService.EndTurnRequested -= this.EndTurnService_EndTurnRequested;
		this.endTurnService = null;
		this.playerControllerservice = null;
	}

	private readonly List<SynchronousJob> synchronousDelegates;

	private int lastJobIndex;

	private ITickableRepositoryAIHelper tickableRepositoryAIHelper;

	private IEventService eventService;

	private IEndTurnService endTurnService;

	private IPlayerControllerRepositoryService playerControllerservice;

	private int lastEndTurnRequestTurn;

	private int numberOfLivingEmpires;

	private int tickLimit;
}
