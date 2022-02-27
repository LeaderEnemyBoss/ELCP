using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommander_RuinHunter : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_RuinHunter(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.RuinHunter, globalObjectiveID, regionIndex)
	{
		this.pointOfInterests = new List<PointOfInterest>();
	}

	public AICommander_RuinHunter() : base(AICommanderMissionDefinition.AICommanderCategory.RuinHunter, 0UL, 0)
	{
		this.pointOfInterests = new List<PointOfInterest>();
	}

	public override bool IsMissionFinished(bool forceStop)
	{
		if (!this.aiDataRepository.IsGUIDValid(base.ForceArmyGUID))
		{
			return true;
		}
		Army army = null;
		bool flag = false;
		Comparison<PointOfInterest> <>9__0;
		for (int i = 0; i < this.pointOfInterests.Count; i++)
		{
			if (ELCPUtilities.CanSearch(base.Empire, this.pointOfInterests[0], this.questManagementService, this.questRepositoryService) && this.departmentOfForeignAffairs.CanMoveOn(this.pointOfInterests[0].Region.Index, false))
			{
				return false;
			}
			this.pointOfInterests.RemoveAt(i);
			if (!flag)
			{
				if (!this.gameEntityRepositoryService.TryGetValue<Army>(base.ForceArmyGUID, out army))
				{
					return true;
				}
				flag = true;
			}
			List<PointOfInterest> list = this.pointOfInterests;
			Comparison<PointOfInterest> comparison;
			if ((comparison = <>9__0) == null)
			{
				comparison = (<>9__0 = ((PointOfInterest left, PointOfInterest right) => this.worldPositionningService.GetDistance(left.WorldPosition, army.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, army.WorldPosition))));
			}
			list.Sort(comparison);
			i--;
		}
		return true;
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
	}

	public override void PopulateMission()
	{
		if (base.Missions.Count == 0)
		{
			Tags tags = new Tags();
			tags.AddTag(base.Category.ToString());
			base.PopulationFirstMissionFromCategory(tags, new object[0]);
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
		this.questManagementService = null;
		this.questRepositoryService = null;
		this.aiDataRepository = null;
		this.pointOfInterests = null;
		this.worldPositionningService = null;
		this.gameEntityRepositoryService = null;
		this.departmentOfForeignAffairs = null;
	}

	public override float GetPriority(AICommanderMission mission)
	{
		return 0.3f;
	}

	public List<PointOfInterest> PointOfInterests
	{
		get
		{
			return this.pointOfInterests;
		}
		set
		{
			this.pointOfInterests = value;
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("PointOfInterests");
		writer.WriteAttributeString<int>("Count", this.pointOfInterests.Count);
		for (int i = 0; i < this.pointOfInterests.Count; i++)
		{
			writer.WriteElementString<ulong>("PointOfInterestGUID", this.pointOfInterests[i].GUID);
		}
		writer.WriteEndElement();
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		if (reader.IsStartElement("PointOfInterests"))
		{
			IGameEntityRepositoryService service = Services.GetService<IGameService>().Game.Services.GetService<IGameEntityRepositoryService>();
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("PointOfInterests");
			this.pointOfInterests.Clear();
			for (int i = 0; i < attribute; i++)
			{
				GameEntityGUID guid = reader.ReadElementString<ulong>("PointOfInterestGUID");
				PointOfInterest item = null;
				if (service.TryGetValue<PointOfInterest>(guid, out item))
				{
					this.pointOfInterests.Add(item);
				}
			}
			reader.ReadEndElement("PointOfInterests");
		}
	}

	private List<PointOfInterest> pointOfInterests;

	private IQuestManagementService questManagementService;

	private IAIDataRepositoryAIHelper aiDataRepository;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IWorldPositionningService worldPositionningService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IQuestRepositoryService questRepositoryService;
}
