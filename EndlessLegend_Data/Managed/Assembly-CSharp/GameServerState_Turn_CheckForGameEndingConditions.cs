using System;
using System.Collections.Generic;
using System.Text;
using Amplitude;
using Amplitude.Unity.Framework;

public class GameServerState_Turn_CheckForGameEndingConditions : GameServerState
{
	public GameServerState_Turn_CheckForGameEndingConditions(GameServer gameServer) : base(gameServer)
	{
	}

	private int TurnToCheckTheGameEndingConditionsFor { get; set; }

	public override void Begin(params object[] parameters)
	{
		if (TutorialManager.IsActivated)
		{
			ITutorialService service = TutorialManager.GetService();
			if (service != null && !service.GetValue<bool>(TutorialManager.EnableVictoryKey, true))
			{
				base.GameServer.PostStateChange(typeof(GameServerState_Turn_AI), new object[0]);
				return;
			}
		}
		base.Begin(parameters);
		if (Amplitude.Unity.Framework.Application.Version.Accessibility <= Accessibility.Internal)
		{
			Diagnostics.Log("GameServerState_Turn_CheckForGameEndingConditions.");
		}
		IVictoryManagementService service2 = base.GameServer.Game.Services.GetService<IVictoryManagementService>();
		if (service2 != null)
		{
			this.TurnToCheckTheGameEndingConditionsFor = base.GameServer.Game.Turn - 1;
			bool flag = false;
			VictoryManager victoryManager = service2 as VictoryManager;
			if (victoryManager != null)
			{
				victoryManager.OnBeginTurn();
			}
			this.CheckForEliminations(service2);
			this.CheckForVictoryConditions(service2, out flag);
			if (flag)
			{
				base.GameServer.PostStateChange(typeof(GameServerState_Turn_DealWithGameEndingConditions), new object[0]);
				return;
			}
		}
		base.GameServer.PostStateChange(typeof(GameServerState_Turn_AI), new object[0]);
	}

	private void CheckForEliminations(IVictoryManagementService victoryManagementService)
	{
		foreach (Empire empire in base.GameServer.Game.Empires)
		{
			MajorEmpire majorEmpire = empire as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			if (majorEmpire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
			{
				foreach (Empire empire2 in base.GameServer.Game.Empires)
				{
					empire2.OnEmpireEliminated(majorEmpire, true);
				}
			}
		}
	}

	private void CheckForVictoryConditions(IVictoryManagementService victoryManagementService, out bool gameEnding)
	{
		gameEnding = false;
		if (victoryManagementService != null && victoryManagementService.HasAlreadyWon)
		{
			return;
		}
		if (base.GameServer.Game.Turn > 0 && victoryManagementService != null)
		{
			try
			{
				victoryManagementService.VictoryConditionRaised += this.VictoryManagementService_VictoryConditionRaised;
				this.victoryConditionsRaised.Clear();
				victoryManagementService.CheckForVictoryConditions(this.TurnToCheckTheGameEndingConditionsFor);
			}
			catch
			{
			}
			finally
			{
				victoryManagementService.VictoryConditionRaised -= this.VictoryManagementService_VictoryConditionRaised;
			}
			if (this.victoryConditionsRaised.Count > 0)
			{
				Dictionary<string, List<Empire>> dictionary = new Dictionary<string, List<Empire>>();
				foreach (VictoryConditionRaisedEventArgs victoryConditionRaisedEventArgs in this.victoryConditionsRaised)
				{
					if (!(victoryConditionRaisedEventArgs.VictoryCondition.Category != VictoryCondition.ReadOnlyVictory))
					{
						List<Empire> list = null;
						if (!dictionary.TryGetValue(victoryConditionRaisedEventArgs.VictoryCondition.Name, out list))
						{
							list = new List<Empire>();
							dictionary.Add(victoryConditionRaisedEventArgs.VictoryCondition.Name, list);
						}
						list.Add(victoryConditionRaisedEventArgs.Empire);
					}
				}
				if (dictionary.Count > 0)
				{
					StringBuilder stringBuilder = new StringBuilder(1024);
					foreach (KeyValuePair<string, List<Empire>> keyValuePair in dictionary)
					{
						if (stringBuilder.Length > 0)
						{
							stringBuilder.Append('&');
						}
						stringBuilder.Append(keyValuePair.Key);
						for (int i = 0; i < keyValuePair.Value.Count; i++)
						{
							stringBuilder.Append(',');
							stringBuilder.Append(keyValuePair.Value[i].Index);
						}
					}
					base.GameServer.Session.SetLobbyData("Victory", stringBuilder.ToString(), true);
					gameEnding = true;
					victoryManagementService.HasAlreadyWon = true;
				}
			}
		}
	}

	private void VictoryManagementService_VictoryConditionRaised(object sender, VictoryConditionRaisedEventArgs e)
	{
		Diagnostics.Log("Victory condition (name: '{0}') was raised for empire (index: {1}, name: '{2}').", new object[]
		{
			e.VictoryCondition.Name,
			e.Empire.Index,
			e.Empire.Name
		});
		this.victoryConditionsRaised.Add(e);
	}

	private List<VictoryConditionRaisedEventArgs> victoryConditionsRaised = new List<VictoryConditionRaisedEventArgs>();
}
