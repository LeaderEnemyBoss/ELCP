using System;
using System.Collections;
using System.IO;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;

public class GameServerState_GameLaunchedAndReady : GameServerState
{
	public GameServerState_GameLaunchedAndReady(GameServer gameServer) : base(gameServer)
	{
	}

	public override void Begin(params object[] parameters)
	{
		base.Begin(parameters);
		Diagnostics.Log("GameServerState_GameLaunchedAndReady.");
		this.coroutine = Coroutine.StartCoroutine(this.RunAsync(), null);
	}

	public override void End(bool abort)
	{
		this.coroutine = null;
		Diagnostics.Log("[Net][GameServer] GameInProgress");
		base.GameServer.GameInProgress = true;
		base.End(abort);
	}

	public override void Run()
	{
		base.Run();
		this.coroutine.Run();
		if (this.coroutine.IsFinished)
		{
			base.GameServer.PostStateChange(typeof(GameServerState_Turn_Begin), new object[0]);
		}
	}

	private IEnumerator RunAsync()
	{
		IPlayerControllerRepositoryService playerControllerRepositoryService = base.GameServer.Game.GetService<IPlayerControllerRepositoryService>();
		if (playerControllerRepositoryService != null)
		{
			for (int index = 0; index < base.GameServer.Game.Empires.Length; index++)
			{
				PlayerController playerController = new PlayerController(base.GameServer)
				{
					Empire = base.GameServer.Game.Empires[index],
					PlayerID = base.GameServer.Game.Empires[index].PlayerID
				};
				playerControllerRepositoryService.Register(playerController);
				base.GameServer.Game.Empires[index].PlayerControllers.Server = playerController;
			}
		}
		if (playerControllerRepositoryService != null)
		{
			PlayerController playerController2 = new PlayerController(base.GameServer)
			{
				PlayerID = "server"
			};
			playerControllerRepositoryService.Register(playerController2);
		}
		base.GameServer.BindMajorEmpires();
		Diagnostics.Assert(base.GameServer != null);
		Diagnostics.Assert(base.GameServer.Session != null);
		switch (base.GameServer.Session.SessionMode)
		{
		case SessionMode.Private:
		case SessionMode.Protected:
		case SessionMode.Public:
		{
			IGameSerializationService gameSerializationService = Services.GetService<IGameSerializationService>();
			if (gameSerializationService != null && gameSerializationService.GameSaveDescriptor == null)
			{
				Diagnostics.Log("Performing a quick save for the clients to download...");
				string title = "%QuickSaveFileName";
				string outputFileName = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.TempDirectory, "Quicksave.sav");
				yield return gameSerializationService.SaveGameAsync(title, outputFileName, GameSaveOptions.QuickSave);
			}
			yield return this.SynchronizeOnClientConnections();
			break;
		}
		}
		while (!base.VerifyGameClientStateSynchronization<GameClientState_GameLaunchedAndReady>())
		{
			base.RespondToDownloadGameRequests();
			yield return null;
		}
		yield break;
	}

	private IEnumerator SynchronizeOnClientConnections()
	{
		bool synced;
		do
		{
			synced = true;
			Steamworks.SteamID[] members = base.GameServer.Session.GetLobbyMembers();
			for (int index = 0; index < members.Length; index++)
			{
				bool memberIsReady = base.GameServer.Session.GetLobbyMemberData<bool>(members[index], "Ready", false);
				if (memberIsReady && !base.GameServer.GameClientConnections.Keys.Contains(members[index]))
				{
					synced = false;
					break;
				}
			}
			if (!synced)
			{
				Diagnostics.Log("[Net][Gameserver] Some clients are not connected yet. Waiting for a while.");
				yield return Coroutine.WaitForSeconds(0.5f);
			}
		}
		while (!synced);
		yield break;
	}

	private Coroutine coroutine;
}
