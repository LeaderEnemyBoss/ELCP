using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommander_QuestBTCommander : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_QuestBTCommander(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.QuestBTOrder, globalObjectiveID, regionIndex)
	{
	}

	public AICommander_QuestBTCommander() : base(AICommanderMissionDefinition.AICommanderCategory.QuestBTOrder, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		this.QuestName = reader.GetAttribute<string>("QuestName");
		this.ForceFinish = reader.GetAttribute<bool>("ForceFinish");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<string>("QuestName", this.QuestName);
		writer.WriteAttributeString<bool>("ForceFinish", this.ForceFinish);
		base.WriteXml(writer);
	}

	public StaticString QuestName { get; set; }

	public override bool IsMissionFinished(bool forceStep)
	{
		IGameEntity gameEntity;
		return this.ForceFinish || !this.questManagementService.IsQuestRunningForEmpire(this.QuestName, base.Empire) || !this.gameEntityRepositoryService.TryGetValue<IGameEntity>(base.SubObjectiveGuid, out gameEntity);
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
	}

	public override void PopulateMission()
	{
		if (base.Missions.Count > 0)
		{
			return;
		}
		Tags tags = new Tags();
		tags.AddTag(base.Category.ToString());
		base.PopulationFirstMissionFromCategory(tags, new object[]
		{
			base.SubObjectiveGuid
		});
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		this.PopulateMission();
		base.PromoteMission();
	}

	public override void Release()
	{
		base.Release();
		this.gameEntityRepositoryService = null;
		this.questManagementService = null;
		this.QuestName = null;
	}

	public bool ForceFinish { get; set; }

	private IQuestManagementService questManagementService;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
