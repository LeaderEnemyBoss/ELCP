using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Achievement;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class AchievementPrerequisite : Prerequisite
{
	[XmlAttribute]
	public QuestAchievement QuestAchievement { get; set; }

	public override bool Check(SimulationObject simulationObject)
	{
		bool flag = this.CheckOnSimulationObject(simulationObject);
		if (base.Inverted)
		{
			flag = !flag;
		}
		return flag;
	}

	public override bool Check(InterpreterContext context)
	{
		return this.Check(context.SimulationObject);
	}

	private bool CheckOnSimulationObject(SimulationObject simulationObject)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			global::Game game = service.Game as global::Game;
			if (game != null && game.Empires != null)
			{
				global::Empire empire = game.Empires.FirstOrDefault((global::Empire match) => match is MajorEmpire && match.SimulationObject.Name == simulationObject.Name);
				if (empire != null)
				{
					if (empire.IsControlledByAI)
					{
						return false;
					}
					if (this.sessionService == null)
					{
						this.sessionService = Services.GetService<ISessionService>();
						Diagnostics.Assert(this.sessionService != null && this.sessionService.Session != null);
					}
					if (this.sessionService.Session.SessionMode == SessionMode.Single)
					{
						if (this.achievementService == null)
						{
							this.achievementService = Services.GetService<IAchievementService>();
							Diagnostics.Assert(this.achievementService != null);
						}
						return this.achievementService.GetAchievement(this.QuestAchievement.ToString());
					}
					if (this.playerRepositoryService == null)
					{
						this.playerRepositoryService = service.Game.Services.GetService<IPlayerRepositoryService>();
						Diagnostics.Assert(this.playerRepositoryService != null);
					}
					Player[] playersByEmpireIndex = this.playerRepositoryService.GetPlayersByEmpireIndex(empire.Index);
					uint num = 1u << (int)this.QuestAchievement;
					foreach (Player player in playersByEmpireIndex)
					{
						uint lobbyMemberData = this.sessionService.Session.GetLobbyMemberData<uint>(player.SteamID, "QuestAchievementsCompletion", 0u);
						if ((lobbyMemberData & num) == 0u)
						{
							return false;
						}
					}
					if (playersByEmpireIndex.Length > 0)
					{
						return true;
					}
				}
			}
		}
		return false;
	}

	private IPlayerRepositoryService playerRepositoryService;

	private ISessionService sessionService;

	private IAchievementService achievementService;
}
