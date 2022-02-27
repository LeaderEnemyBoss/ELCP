using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommander_QuestSolverOrder : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_QuestSolverOrder(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.QuestSolverOrder, globalObjectiveID, regionIndex)
	{
	}

	public AICommander_QuestSolverOrder() : base(AICommanderMissionDefinition.AICommanderCategory.QuestSolverOrder, 0UL, 0)
	{
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		IGameEntity gameEntity;
		return this.ForceFinish || !this.aiDataRepository.IsGUIDValid(base.ForceArmyGUID) || !this.questManagementService.IsQuestRunningForEmpire(this.QuestName, base.Empire) || !this.gameEntityRepositoryService.TryGetValue<IGameEntity>(base.SubObjectiveGuid, out gameEntity);
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
	}

	public override void PopulateMission()
	{
		IGameEntity gameEntity;
		if (base.Missions.Count == 0 && this.gameEntityRepositoryService.TryGetValue(base.SubObjectiveGuid, out gameEntity) && base.ForceArmyGUID.IsValid)
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
		this.aiDataRepository = null;
		this.QuestName = null;
	}

	public override float GetPriority(AICommanderMission mission)
	{
		return 2f;
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<string>("QuestName", this.QuestName);
		writer.WriteAttributeString<bool>("ForceFinish", this.ForceFinish);
		base.WriteXml(writer);
	}

	public override void ReadXml(XmlReader reader)
	{
		this.QuestName = reader.GetAttribute<string>("QuestName");
		this.ForceFinish = reader.GetAttribute<bool>("ForceFinish");
		base.ReadXml(reader);
	}

	public StaticString QuestName { get; set; }

	public bool ForceFinish { get; set; }

	private IQuestManagementService questManagementService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IAIDataRepositoryAIHelper aiDataRepository;
}
