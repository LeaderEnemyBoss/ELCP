using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;

[Diagnostics.TagAttribute("AI")]
public class SynchronousJobRepository : AIHelper, ISynchronousJobRepositoryAIHelper, IService, ITickable
{
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
		while (this.lastJobIndex >= 0 && num++ <= 5)
		{
			SynchronousJob synchronousJob = this.synchronousDelegates[this.lastJobIndex];
			if (synchronousJob == null)
			{
				goto IL_AB;
			}
			switch (synchronousJob())
			{
			case SynchronousJobState.Running:
				break;
			case SynchronousJobState.Success:
				goto IL_AB;
			case SynchronousJobState.Failure:
				Diagnostics.Log("Synchronous job {0} failed.", new object[]
				{
					synchronousJob.Method.Name
				});
				goto IL_AB;
			default:
				goto IL_AB;
			}
			IL_9B:
			this.lastJobIndex--;
			continue;
			IL_AB:
			this.synchronousDelegates.RemoveAt(this.lastJobIndex);
			goto IL_9B;
		}
	}

	private readonly List<SynchronousJob> synchronousDelegates = new List<SynchronousJob>();

	private int lastJobIndex;

	private ITickableRepositoryAIHelper tickableRepositoryAIHelper;
}
