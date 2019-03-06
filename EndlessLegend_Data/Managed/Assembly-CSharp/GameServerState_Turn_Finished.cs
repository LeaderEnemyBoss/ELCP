using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Messaging;

public class GameServerState_Turn_Finished : GameServerState<GameServerState_Turn_Finished>
{
	public GameServerState_Turn_Finished(GameServer gameServer) : base(gameServer)
	{
	}

	~GameServerState_Turn_Finished()
	{
		this.BattleEncounterRepositoryService = null;
	}

	[Ancillary]
	private IBattleEncounterRepositoryService BattleEncounterRepositoryService { get; set; }

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		this.BattleEncounterRepositoryService = base.GameServer.Game.GetService<IBattleEncounterRepositoryService>();
		if (AIScheduler.Services != null)
		{
			this.tickableRepository = (AIScheduler.Services.GetService<ITickableRepositoryAIHelper>() as TickableRepository);
		}
		this.coroutine = Coroutine.StartCoroutine(this.RunAsync(), null);
	}

	public override void End(bool abort)
	{
		base.End(abort);
		this.BattleEncounterRepositoryService = null;
		this.tickableRepository = null;
	}

	public override void Run()
	{
		base.Run();
		this.UpdateEncounters();
		this.coroutine.Run();
		if (this.coroutine.IsFinished)
		{
			base.GameServer.PostStateChange(typeof(GameServerState_Turn_End), new object[0]);
		}
	}

	private IEnumerator RunAsync()
	{
		yield return this.WaitForAI();
		List<Coroutine> coroutines = new List<Coroutine>();
		for (int index = 0; index < base.GameServer.Game.Empires.Length; index++)
		{
			Empire empire = base.GameServer.Game.Empires[index];
			Coroutine coroutine = Coroutine.StartCoroutine(empire.DoPasses("GameServerState_Turn_Finished"), null);
			if (!coroutine.IsFinished)
			{
				coroutines.Add(coroutine);
			}
		}
		while (coroutines.Count > 0)
		{
			for (int index2 = coroutines.Count - 1; index2 >= 0; index2--)
			{
				Coroutine coroutine2 = coroutines[index2];
				coroutine2.Run();
				if (coroutine2.IsFinished)
				{
					coroutines.RemoveAt(index2);
				}
			}
			yield return null;
		}
		GameServerState_Turn_Finished.SynchronisationFlags synchronisationFlags;
		do
		{
			synchronisationFlags = GameServerState_Turn_Finished.SynchronisationFlags.None;
			yield return Coroutine.WaitForNumberOfFrames(4u);
			if (base.GameServer.HasPendingOrders)
			{
				synchronisationFlags |= GameServerState_Turn_Finished.SynchronisationFlags.ServerSidePendingOrders;
			}
			else
			{
				if (this.BattleEncounterRepositoryService != null)
				{
					foreach (BattleEncounter battleEncounter in this.BattleEncounterRepositoryService)
					{
						if (!battleEncounter.IsBattleFinished)
						{
							synchronisationFlags |= GameServerState_Turn_Finished.SynchronisationFlags.ServerSidePendingBattleEncounters;
							break;
						}
					}
					if (synchronisationFlags != GameServerState_Turn_Finished.SynchronisationFlags.None)
					{
						goto IL_2A1;
					}
				}
				if (!base.VerifyGameClientOrderProcessingSynchronization(false))
				{
					synchronisationFlags |= GameServerState_Turn_Finished.SynchronisationFlags.ClientSidePendingOrders;
				}
			}
			IL_2A1:;
		}
		while (synchronisationFlags != GameServerState_Turn_Finished.SynchronisationFlags.None);
		Message message = new GameServerPostStateChangeMessage(typeof(GameClientState_Turn_FinishedAndLocked));
		base.GameServer.SendMessageToClients(ref message, true);
		while (!base.VerifyGameClientStateSynchronization<GameClientState_Turn_FinishedAndLocked>())
		{
			yield return null;
		}
		yield return Coroutine.WaitForNumberOfFrames(4u);
		while (base.GameServer.HasPendingOrders)
		{
			yield return null;
		}
		yield break;
	}

	private IEnumerator WaitForAI()
	{
		DateTime time = DateTime.Now;
		double timeout = 2.5;
		if (base.GameServer.AIScheduler != null)
		{
			while (!base.GameServer.AIScheduler.CanEndTurn())
			{
				this.tickableRepository.Tick(false);
				yield return null;
				TimeSpan timeSpan = DateTime.Now - time;
				double totalSeconds = timeSpan.TotalSeconds;
				if (global::Application.FantasyPreferences.ForceAIEndTurn && timeSpan.TotalSeconds > timeout)
				{
					Diagnostics.LogWarning("Timeout! The AI scheduler is taking too long allowing for the turn to end... aborting!");
					Diagnostics.LogWarning("Timeout: Tickables NeedTick: " + this.tickableRepository.Tickables.ToString(delegate(ITickable tickable)
					{
						if (tickable.State == TickableState.NeedTick)
						{
							return tickable.GetType().ToString();
						}
						return string.Empty;
					}, ", ", string.Empty));
					Diagnostics.LogWarning("Timeout: Tickables NeedTick Count: " + this.tickableRepository.Tickables.Count((ITickable tickable) => tickable.State == TickableState.NeedTick));
					break;
				}
			}
		}
		yield break;
	}

	private IEnumerator WaitForBattleEncountersToEnd()
	{
		if (this.BattleEncounterRepositoryService == null)
		{
			yield break;
		}
		bool atLeastOneBattleEncounterIsPending = false;
		do
		{
			yield return null;
			atLeastOneBattleEncounterIsPending = false;
			foreach (BattleEncounter battleEncounter in this.BattleEncounterRepositoryService)
			{
				if (!battleEncounter.IsBattleFinished)
				{
					atLeastOneBattleEncounterIsPending = true;
					break;
				}
			}
		}
		while (atLeastOneBattleEncounterIsPending);
		this.BattleEncounterRepositoryService.Clear();
		yield break;
	}

	private void UpdateEncounters()
	{
		foreach (BattleEncounter battleEncounter in this.BattleEncounterRepositoryService)
		{
			battleEncounter.Update();
		}
	}

	private TickableRepository tickableRepository;

	private Coroutine coroutine;

	[Flags]
	private enum SynchronisationFlags
	{
		None = 0,
		ServerSidePendingOrders = 268435457,
		ServerSidePendingBattleEncounters = 268435458,
		ClientSidePendingOrders = 536870913
	}
}
