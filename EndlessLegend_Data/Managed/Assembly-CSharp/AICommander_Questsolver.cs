using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml.Serialization;

public class AICommander_Questsolver : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_Questsolver(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.Questsolver, globalObjectiveID, regionIndex)
	{
	}

	public AICommander_Questsolver() : base(AICommanderMissionDefinition.AICommanderCategory.Questsolver, 0UL, 0)
	{
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		if (!this.aiDataRepository.IsGUIDValid(base.ForceArmyGUID))
		{
			return true;
		}
		if (!this.questManagementService.IsQuestRunningForEmpire(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestWingsOfRuinName, base.Empire))
		{
			return true;
		}
		if (this.questBehaviour == null)
		{
			this.questBehaviour = this.questRepositoryService.GetQuestBehaviour(AILayer_QuestSolver.ImportantQuestNames.GlobalQuestWingsOfRuinName, base.Empire.Index);
		}
		return this.questBehaviour == null || this.questBehaviour.Quest.GetCurrentStepIndex() > 0;
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
	}

	public override void PopulateMission()
	{
		IGameEntity gameEntity;
		if (base.Missions.Count == 0 && this.gameEntityRepositoryService.TryGetValue(base.SubObjectiveGuid, out gameEntity) && gameEntity is PointOfInterest)
		{
			Tags tags = new Tags();
			tags.AddTag(base.Category.ToString());
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				base.SubObjectiveGuid
			});
		}
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		if (base.Missions.Count == 0 && !this.IsMissionFinished(false))
		{
			this.PopulateMission();
			base.PromoteMission();
		}
	}

	public override void Release()
	{
		base.Release();
		this.gameEntityRepositoryService = null;
		this.questManagementService = null;
		this.questRepositoryService = null;
		this.questBehaviour = null;
		this.aiDataRepository = null;
	}

	public override float GetPriority(AICommanderMission mission)
	{
		return 2f;
	}

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private QuestBehaviour questBehaviour;

	private IAIDataRepositoryAIHelper aiDataRepository;
}
