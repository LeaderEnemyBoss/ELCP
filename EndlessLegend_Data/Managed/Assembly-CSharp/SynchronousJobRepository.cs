using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;

[Diagnostics.TagAttribute("AI")]
public class SynchronousJobRepository : AIHelper, ISynchronousJobRepositoryAIHelper, ITickable, IService
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
		while (this.lastJobIndex >= 0)
		{
			if (num++ > 10)
			{
				break;
			}
			SynchronousJob synchronousJob = this.synchronousDelegates[this.lastJobIndex];
			if (synchronousJob == null)
			{
				goto IL_C5;
			}
			switch (synchronousJob())
			{
			case SynchronousJobState.Running:
				break;
			case SynchronousJobState.Success:
				goto IL_C5;
			case SynchronousJobState.Failure:
				Diagnostics.Log("Synchronous job {0} failed.", new object[]
				{
					synchronousJob.Method.Name
				});
				goto IL_C5;
			default:
				goto IL_C5;
			}
			IL_D6:
			this.lastJobIndex--;
			continue;
			IL_C5:
			this.synchronousDelegates.RemoveAt(this.lastJobIndex);
			goto IL_D6;
		}
	}

	private readonly List<SynchronousJob> synchronousDelegates = new List<SynchronousJob>();

	private int lastJobIndex;

	private ITickableRepositoryAIHelper tickableRepositoryAIHelper;
}
