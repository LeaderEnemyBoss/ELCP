using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_Pacification : AILayerWithObjective, IXmlSerializable
{
	public AILayer_Pacification() : base("Pacification")
	{
		this.colonizationObjectives = new List<GlobalObjectiveMessage>();
		this.QuestBTPacifications = new List<AILayer_Pacification.QuestBTPacification>();
	}

	public static Army GetMaxHostileArmy(global::Empire empire, int regionIndex)
	{
		Services.GetService<IGameService>().Game.Services.GetService<IQuestManagementService>();
		Army army = null;
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			return null;
		}
		foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army2.Empire != empire && !army2.IsSeafaring && agency.CanAttack(army2) && (army == null || army2.GetPropertyValue(SimulationProperties.MilitaryPower) > army.GetPropertyValue(SimulationProperties.MilitaryPower)))
			{
				army = army2;
			}
		}
		return army;
	}

	public static bool RegionContainsHostileArmies(global::Empire empire, int regionIndex)
	{
		if (empire == null)
		{
			return false;
		}
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency == null)
		{
			return false;
		}
		bool flag = agency.IsInWarWithSomeone();
		IGameService service = Services.GetService<IGameService>();
		service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(service != null);
		Region region = service.Game.Services.GetService<IWorldPositionningService>().GetRegion(regionIndex);
		if (flag && region != null && (region.City == null || region.City.Empire != empire))
		{
			if (region.City == null)
			{
				foreach (Army army in Intelligence.GetArmiesInRegion(regionIndex))
				{
					if ((army.Empire is MinorEmpire || army.Empire is LesserEmpire) && agency.CanAttack(army))
					{
						return true;
					}
				}
			}
			return false;
		}
		foreach (Army army2 in Intelligence.GetVisibleArmiesInRegion(regionIndex, empire))
		{
			if (army2.Empire != empire && agency.CanAttack(army2))
			{
				return true;
			}
		}
		return false;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Pacification_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[]
		{
			"AILayer_Colonization_RefreshObjectives"
		});
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfTheInterior = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfDefense = null;
		this.pathfindingService = null;
		this.worldPositionningService = null;
		this.questManagementService = null;
		this.questRepositoryService = null;
		this.gameEntityRepositoryService = null;
		this.colonizationObjectives.Clear();
		this.colonizationObjectives = null;
		this.QuestBTPacifications.Clear();
	}

	protected override int GetCommanderLimit()
	{
		int num = this.departmentOfTheInterior.Cities.Count + 1;
		if (num < base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count / 4)
		{
			num = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>().Armies.Count / 4;
		}
		return num;
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex);
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Pacification.ToString(), false, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		base.GatherObjectives(AICommanderMissionDefinition.AICommanderCategory.Colonization.ToString(), ref this.colonizationObjectives);
		this.colonizationObjectives.Sort((GlobalObjectiveMessage left, GlobalObjectiveMessage right) => -1 * left.LocalPriority.CompareTo(right.LocalPriority));
		List<int> list = new List<int>();
		for (int l = 0; l < this.colonizationObjectives.Count; l++)
		{
			int regionIndex = this.colonizationObjectives[l].RegionIndex;
			list.Add(this.colonizationObjectives[l].RegionIndex);
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
			{
				GlobalObjectiveMessage item = base.GenerateObjective(regionIndex);
				this.globalObjectiveMessages.Add(item);
			}
		}
		for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
		{
			int regionIndex = this.departmentOfTheInterior.Cities[j].Region.Index;
			list.AddOnce(this.departmentOfTheInterior.Cities[j].Region.Index);
			if (this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex) == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex))
			{
				GlobalObjectiveMessage item2 = base.GenerateObjective(regionIndex);
				this.globalObjectiveMessages.Add(item2);
			}
		}
		this.ComputeObjectivePriority();
		if (this.questManagementService.IsQuestRunningForEmpire("VictoryQuest-Chapter2", base.AIEntity.Empire))
		{
			QuestBehaviour questBehaviour = this.questRepositoryService.GetQuestBehaviour("VictoryQuest-Chapter2", base.AIEntity.Empire.Index);
			QuestBehaviourTreeNode_Action_SpawnArmy questBehaviourTreeNode_Action_SpawnArmy;
			if (questBehaviour != null && ELCPUtilities.TryGetFirstNodeOfType<QuestBehaviourTreeNode_Action_SpawnArmy>(questBehaviour.Root as BehaviourTreeNodeController, out questBehaviourTreeNode_Action_SpawnArmy))
			{
				Region region = this.worldPositionningService.GetRegion(questBehaviourTreeNode_Action_SpawnArmy.SpawnLocations[0]);
				int regionIndex = region.Index;
				GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages.Find((GlobalObjectiveMessage match) => match.RegionIndex == regionIndex);
				list.AddOnce(regionIndex);
				if (globalObjectiveMessage == null && AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, regionIndex) && (region.City == null || region.City.Empire == base.AIEntity.Empire))
				{
					globalObjectiveMessage = base.GenerateObjective(regionIndex);
					this.globalObjectiveMessages.Add(globalObjectiveMessage);
				}
				if (globalObjectiveMessage != null)
				{
					globalObjectiveMessage.GlobalPriority.Value = 1f;
					globalObjectiveMessage.LocalPriority.Value = 1f;
				}
			}
		}
		float value = 0.9f;
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			value = 0.7f;
		}
		int i;
		Predicate<GlobalObjectiveMessage> <>9__4;
		int i2;
		for (i = 0; i < this.QuestBTPacifications.Count; i = i2 + 1)
		{
			if (!this.questManagementService.IsQuestRunningForEmpire(this.QuestBTPacifications[i].questName, base.AIEntity.Empire))
			{
				this.QuestBTPacifications.RemoveAt(i);
				i2 = i;
				i = i2 - 1;
			}
			else if (!list.Contains(this.QuestBTPacifications[i].regionIndex))
			{
				Region region2 = this.worldPositionningService.GetRegion(this.QuestBTPacifications[i].regionIndex);
				if (region2.Owner == null || !(region2.Owner is MajorEmpire))
				{
					list.AddOnce(this.QuestBTPacifications[i].regionIndex);
					if (this.CanPathToObjective(region2))
					{
						List<GlobalObjectiveMessage> globalObjectiveMessages = this.globalObjectiveMessages;
						Predicate<GlobalObjectiveMessage> match2;
						if ((match2 = <>9__4) == null)
						{
							match2 = (<>9__4 = ((GlobalObjectiveMessage match) => match.RegionIndex == this.QuestBTPacifications[i].regionIndex));
						}
						GlobalObjectiveMessage globalObjectiveMessage2 = globalObjectiveMessages.Find(match2);
						if (globalObjectiveMessage2 != null)
						{
							globalObjectiveMessage2.GlobalPriority.Value = value;
							globalObjectiveMessage2.LocalPriority.Value = value;
						}
						else if (AILayer_Pacification.RegionContainsHostileArmies(base.AIEntity.Empire, this.QuestBTPacifications[i].regionIndex))
						{
							globalObjectiveMessage2 = base.GenerateObjective(this.QuestBTPacifications[i].regionIndex);
							this.globalObjectiveMessages.Add(globalObjectiveMessage2);
							globalObjectiveMessage2.GlobalPriority.Value = value;
							globalObjectiveMessage2.LocalPriority.Value = value;
						}
					}
				}
			}
			i2 = i;
		}
		for (int k = 0; k < this.globalObjectiveMessages.Count; k++)
		{
			if ((this.globalObjectiveMessages[k].State == BlackboardMessage.StateValue.Message_None || this.globalObjectiveMessages[k].State == BlackboardMessage.StateValue.Message_InProgress) && !list.Contains(this.globalObjectiveMessages[k].RegionIndex))
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(this.globalObjectiveMessages[k]);
				this.CancelObjective(this.globalObjectiveMessages[k]);
				this.globalObjectiveMessages.RemoveAt(k);
				k--;
			}
		}
	}

	private void ComputeObjectivePriority()
	{
		base.GlobalPriority.Reset();
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		base.GlobalPriority.Add(layer.StrategicNetwork.GetAgentValue("Pacification"), "Startegic network 'Pacification'", new object[0]);
		for (int i = 0; i < this.globalObjectiveMessages.Count; i++)
		{
			GlobalObjectiveMessage globalObjectiveMessage = this.globalObjectiveMessages[i];
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add(0.5f, "(constant)", new object[0]);
			Region region = this.worldPositionningService.GetRegion(globalObjectiveMessage.RegionIndex);
			if (region.City != null && region.City.Empire == base.AIEntity.Empire)
			{
				if (base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>().IsInWarWithSomeone())
				{
					heuristicValue.Boost(0.5f, "At war", new object[0]);
				}
				if ((float)region.City.UnitsCount < (float)region.City.MaximumUnitSlot * 0.5f)
				{
					heuristicValue.Boost(0.2f, "City defense low", new object[0]);
				}
			}
			globalObjectiveMessage.LocalPriority = heuristicValue;
			globalObjectiveMessage.GlobalPriority = base.GlobalPriority;
			globalObjectiveMessage.TimeOut = 1;
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(1);
		base.WriteXml(writer);
		writer.WriteStartElement("QuestBTPacifications");
		writer.WriteAttributeString<int>("Count", this.QuestBTPacifications.Count);
		for (int i = 0; i < this.QuestBTPacifications.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.QuestBTPacifications[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num > 0 && reader.IsStartElement("QuestBTPacifications"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("QuestBTPacifications");
			this.QuestBTPacifications.Clear();
			for (int i = 0; i < attribute; i++)
			{
				AILayer_Pacification.QuestBTPacification questBTPacification = new AILayer_Pacification.QuestBTPacification("", -1);
				reader.ReadElementSerializable<AILayer_Pacification.QuestBTPacification>(ref questBTPacification);
				if (questBTPacification.IsValid())
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}: Loading QuestBTPacification {1}", new object[]
						{
							base.AIEntity.Empire,
							questBTPacification.ToString()
						});
					}
					this.QuestBTPacifications.Add(questBTPacification);
				}
			}
			reader.ReadEndElement("QuestBTPacifications");
		}
	}

	public void AddQuestBTPacification(StaticString name, int regionindex)
	{
		this.QuestBTPacifications.Add(new AILayer_Pacification.QuestBTPacification(name, regionindex));
	}

	public void RemoveQuestBTPacification(StaticString name, int regionindex)
	{
		this.QuestBTPacifications.RemoveAll((AILayer_Pacification.QuestBTPacification BTo) => BTo.questName == name && BTo.regionIndex == regionindex);
	}

	private bool CanPathToObjective(Region region)
	{
		WorldPosition goal = WorldPosition.Invalid;
		if (region.PointOfInterests != null && region.PointOfInterests.Length != 0)
		{
			goal = region.PointOfInterests[0].WorldPosition;
		}
		else
		{
			goal = region.Barycenter;
		}
		foreach (Army army in this.departmentOfDefense.Armies)
		{
			if (!army.IsSeafaring)
			{
				if (this.pathfindingService.FindPath(army, this.departmentOfTheInterior.Cities[0].WorldPosition, goal, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnorePOI | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
				{
					return false;
				}
				return true;
			}
		}
		return false;
	}

	private List<GlobalObjectiveMessage> colonizationObjectives;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IWorldPositionningService worldPositionningService;

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private List<AILayer_Pacification.QuestBTPacification> QuestBTPacifications;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfDefense departmentOfDefense;

	private IPathfindingService pathfindingService;

	public class QuestBTPacification : IXmlSerializable
	{
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteVersionAttribute(1);
			writer.WriteAttributeString<StaticString>("questName", this.questName);
			writer.WriteAttributeString<int>("regionIndex", this.regionIndex);
		}

		public void ReadXml(XmlReader reader)
		{
			reader.ReadVersionAttribute();
			this.questName = reader.GetAttribute("questName");
			this.regionIndex = reader.GetAttribute<int>("regionIndex");
			reader.ReadStartElement();
		}

		public override string ToString()
		{
			string arg = (this.questName != null) ? this.questName.ToString() : "null";
			return string.Format("{0}:{1}", arg, this.regionIndex);
		}

		public bool IsValid()
		{
			return this.questName != "" && this.regionIndex >= 0;
		}

		public QuestBTPacification(StaticString questname, int regionIndex)
		{
			this.questName = questname;
			this.regionIndex = regionIndex;
		}

		public StaticString questName;

		public int regionIndex;
	}
}
