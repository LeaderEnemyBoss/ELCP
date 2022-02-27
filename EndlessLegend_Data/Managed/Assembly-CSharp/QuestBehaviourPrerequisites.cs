using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;

public class QuestBehaviourPrerequisites : QuestPrerequisites
{
	public QuestBehaviourPrerequisites()
	{
		this.TargetGUID = 0UL;
		this.EmpireIndex = -1;
		this.targetSimulationObjectWrapper = null;
	}

	[XmlElement]
	public ulong TargetGUID { get; set; }

	[XmlElement]
	public int EmpireIndex { get; set; }

	public bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(base.XmlSerializableTarget))
		{
			Diagnostics.LogError("QuestBehaviourPrerequisites: some prerequisite have a null or empty target.");
			return false;
		}
		this.targetSimulationObjectWrapper = this.GetTargetSimulationObjectWrapper(questBehaviour);
		return true;
	}

	public SimulationObjectWrapper GetTargetSimulationObjectWrapper(QuestBehaviour questBehaviour)
	{
		if (base.XmlSerializableTarget == "$Empire")
		{
			this.targetSimulationObjectWrapper = questBehaviour.Initiator;
			return this.targetSimulationObjectWrapper;
		}
		global::Empire empire;
		if (questBehaviour.TryGetQuestVariableValueByName<global::Empire>(base.Target, out empire) && empire != null)
		{
			this.EmpireIndex = empire.Index;
			return empire;
		}
		if (this.EmpireIndex >= 0)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				Diagnostics.LogError("Failed to retrieve the game service.");
				return null;
			}
			global::Game game = service.Game as global::Game;
			if (game == null)
			{
				Diagnostics.LogError("Failed to cast gameService.Game to Game.");
				return null;
			}
			if (game.Empires[this.EmpireIndex] != null)
			{
				return game.Empires[this.EmpireIndex];
			}
		}
		IGameEntity gameEntity;
		if (questBehaviour.TryGetQuestVariableValueByName<IGameEntity>(base.Target, out gameEntity) && gameEntity != null && gameEntity.GUID != GameEntityGUID.Zero && gameEntity is SimulationObjectWrapper)
		{
			this.TargetGUID = gameEntity.GUID;
			this.targetSimulationObjectWrapper = (gameEntity as SimulationObjectWrapper);
			return this.targetSimulationObjectWrapper;
		}
		if (this.TargetGUID != GameEntityGUID.Zero)
		{
			IGameService service2 = Services.GetService<IGameService>();
			if (service2 == null || service2.Game == null)
			{
				Diagnostics.LogError("Failed to retrieve the game service.");
				return null;
			}
			global::Game game2 = service2.Game as global::Game;
			if (game2 == null)
			{
				Diagnostics.LogError("Failed to cast gameService.Game to Game.");
				return null;
			}
			IGameEntityRepositoryService service3 = game2.Services.GetService<IGameEntityRepositoryService>();
			if (service3 == null)
			{
				Diagnostics.LogError("Failed to retrieve the game entity repository service.");
				return null;
			}
			if (service3.TryGetValue(this.TargetGUID, out gameEntity) && gameEntity != null && gameEntity.GUID != GameEntityGUID.Zero && gameEntity is SimulationObjectWrapper)
			{
				this.TargetGUID = gameEntity.GUID;
				this.targetSimulationObjectWrapper = (gameEntity as SimulationObjectWrapper);
				return this.targetSimulationObjectWrapper;
			}
		}
		return null;
	}

	protected SimulationObjectWrapper targetSimulationObjectWrapper;
}
