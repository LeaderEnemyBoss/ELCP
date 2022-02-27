using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AILayer_QuestBTController : AILayerWithObjective, IXmlSerializable
{
	public AILayer_QuestBTController() : base("QuestBTController")
	{
		this.QuestBTOrders = new List<AILayer_QuestBTController.QuestBTOrder>();
		this.messagePriorityOverrides = new Dictionary<ulong, float>();
		this.resourcesNeededForQuest = new Dictionary<StaticString, float>();
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		this.questManagementService = service.Game.Services.GetService<IQuestManagementService>();
		Diagnostics.Assert(this.questManagementService != null);
		this.questRepositoryService = service.Game.Services.GetService<IQuestRepositoryService>();
		Diagnostics.Assert(this.questRepositoryService != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_QuestBTAction_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
		this.pathfindingService = service.Game.Services.GetService<IPathfindingService>();
		this.departmentOfScience = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.aILayer_ArmyManagement = base.AIEntity.GetLayer<AILayer_ArmyManagement>();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.QuestBTOrders.Clear();
		this.messagePriorityOverrides.Clear();
		this.worldPositionningService = null;
		this.departmentOfForeignAffairs = null;
		this.questManagementService = null;
		this.gameEntityRepositoryService = null;
		this.questRepositoryService = null;
		this.pathfindingService = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfScience = null;
		this.departmentOfDefense = null;
		this.departmentOfTheTreasury = null;
		this.aILayer_ArmyManagement = null;
		this.resourcesNeededForQuest.Clear();
	}

	protected override int GetCommanderLimit()
	{
		return 5;
	}

	protected override bool IsObjectiveValid(GlobalObjectiveMessage objective)
	{
		AILayer_QuestBTController.QuestBTOrder questBTOrder;
		return !(objective.ObjectiveType != base.ObjectiveType) && objective.SubObjectifGUID.IsValid && this.TryGetQuestBTOrder(objective.ObjectiveState, objective.SubObjectifGUID, out questBTOrder) && this.questManagementService.IsQuestRunningForEmpire(objective.ObjectiveState, base.AIEntity.Empire) && this.TargetIsReachable(objective.SubObjectifGUID);
	}

	protected override bool IsObjectiveValid(StaticString objectiveType, int regionIndex, bool checkLocalPriority = false)
	{
		return true;
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		base.GatherObjectives(base.ObjectiveType, true, ref this.globalObjectiveMessages);
		base.ValidateMessages(ref this.globalObjectiveMessages);
		this.messagePriorityOverrides.Clear();
		this.resourcesNeededForQuest.Clear();
		if (this.departmentOfTheInterior.Cities.Count == 0)
		{
			return;
		}
		List<StaticString> list = new List<StaticString>();
		using (List<AILayer_QuestBTController.QuestBTOrder>.Enumerator enumerator = this.QuestBTOrders.GetEnumerator())
		{
			while (enumerator.MoveNext())
			{
				AILayer_QuestBTController.QuestBTOrder order = enumerator.Current;
				if (!list.Contains(order.questName))
				{
					List<AILayer_QuestBTController.QuestBTOrder> list2 = this.QuestBTOrders.FindAll((AILayer_QuestBTController.QuestBTOrder q) => q.questName == order.questName);
					if (list2.Count > 1)
					{
						list.Add(order.questName);
						int num = int.MaxValue;
						int num2 = 0;
						int num3 = -1;
						for (int k = 0; k < list2.Count; k++)
						{
							IGameEntity gameEntity;
							if (this.gameEntityRepositoryService.TryGetValue<IGameEntity>(list2[k].objectiveGUID, out gameEntity))
							{
								int distance = this.worldPositionningService.GetDistance(this.departmentOfTheInterior.Cities[0].WorldPosition, (gameEntity as IWorldPositionable).WorldPosition);
								if (distance < num)
								{
									num = distance;
									num3 = k;
								}
								if (distance > num2)
								{
									num2 = distance;
								}
							}
						}
						num2 -= num;
						for (int j = 0; j < list2.Count; j++)
						{
							IGameEntity gameEntity2;
							if (j != num3 && this.gameEntityRepositoryService.TryGetValue<IGameEntity>(list2[j].objectiveGUID, out gameEntity2))
							{
								int num4 = this.worldPositionningService.GetDistance(this.departmentOfTheInterior.Cities[0].WorldPosition, (gameEntity2 as IWorldPositionable).WorldPosition);
								num4 -= num;
								float num5 = 1f - (float)(num4 / num2);
								num5 = Math.Max(num5, 0.3f);
								this.messagePriorityOverrides.Add(list2[j].objectiveGUID, num5 / 1.25f);
							}
						}
					}
				}
			}
		}
		int i;
		Predicate<GlobalObjectiveMessage> <>9__1;
		int i2;
		for (i = this.QuestBTOrders.Count - 1; i >= 0; i = i2 - 1)
		{
			bool flag = this.QuestBTOrders[i].resourceNeeded == StaticString.Empty;
			if (!flag)
			{
				float num6;
				this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, this.QuestBTOrders[i].resourceNeeded, out num6, false);
				flag = (num6 >= (float)this.QuestBTOrders[i].resourceNeededAmount);
				if (this.resourcesNeededForQuest.ContainsKey(this.QuestBTOrders[i].resourceNeeded))
				{
					Dictionary<StaticString, float> dictionary = this.resourcesNeededForQuest;
					StaticString resourceNeeded = this.QuestBTOrders[i].resourceNeeded;
					dictionary[resourceNeeded] += (float)this.QuestBTOrders[i].resourceNeededAmount;
				}
				else
				{
					this.resourcesNeededForQuest.Add(this.QuestBTOrders[i].resourceNeeded, (float)this.QuestBTOrders[i].resourceNeededAmount);
				}
			}
			if (!this.questManagementService.IsQuestRunningForEmpire(this.QuestBTOrders[i].questName, base.AIEntity.Empire))
			{
				this.QuestBTOrders.RemoveAt(i);
			}
			else if (flag)
			{
				List<GlobalObjectiveMessage> globalObjectiveMessages = this.globalObjectiveMessages;
				Predicate<GlobalObjectiveMessage> match;
				if ((match = <>9__1) == null)
				{
					match = (<>9__1 = ((GlobalObjectiveMessage m) => m.ObjectiveState == this.QuestBTOrders[i].questName && m.SubObjectifGUID == this.QuestBTOrders[i].objectiveGUID));
				}
				GlobalObjectiveMessage globalObjectiveMessage = globalObjectiveMessages.Find(match);
				if (globalObjectiveMessage == null && this.TargetIsReachable(this.QuestBTOrders[i].objectiveGUID))
				{
					IGameEntity gameEntity3;
					this.gameEntityRepositoryService.TryGetValue<IGameEntity>(this.QuestBTOrders[i].objectiveGUID, out gameEntity3);
					PointOfInterest pointOfInterest = gameEntity3 as PointOfInterest;
					TerraformDevice terraformDevice = gameEntity3 as TerraformDevice;
					if ((pointOfInterest != null && pointOfInterest.Type != "Village") || this.departmentOfScience.CanParley() || terraformDevice != null)
					{
						Region region = this.worldPositionningService.GetRegion((gameEntity3 as IWorldPositionable).WorldPosition);
						globalObjectiveMessage = this.CreateObjectiveFor(region.Index, gameEntity3, this.QuestBTOrders[i].questName);
					}
				}
				if (globalObjectiveMessage != null)
				{
					this.RefreshMessagePriority(globalObjectiveMessage);
				}
			}
			i2 = i;
		}
	}

	private void RefreshMessagePriority(GlobalObjectiveMessage objectiveMessage)
	{
		HeuristicValue heuristicValue = new HeuristicValue(1f);
		HeuristicValue heuristicValue2 = new HeuristicValue(1f);
		float value;
		if (this.messagePriorityOverrides.TryGetValue(objectiveMessage.SubObjectifGUID, out value))
		{
			heuristicValue.Value = value;
			heuristicValue2.Value = 0.7f;
		}
		objectiveMessage.GlobalPriority = heuristicValue2;
		objectiveMessage.LocalPriority = heuristicValue;
		objectiveMessage.TimeOut = 1;
	}

	private bool TargetIsReachable(GameEntityGUID target)
	{
		IGameEntity gameEntity;
		return this.gameEntityRepositoryService.TryGetValue<IGameEntity>(target, out gameEntity) && this.CanPathToObjective(gameEntity as IWorldPositionable) && (!(gameEntity is PointOfInterest) || this.IsFreeOfBloomsOrFriendly(gameEntity as PointOfInterest));
	}

	private bool CanPathToObjective(IWorldPositionable target)
	{
		if (target == null || this.departmentOfTheInterior.Cities.Count == 0)
		{
			return false;
		}
		foreach (Army army in this.departmentOfDefense.Armies)
		{
			if (!army.IsSeafaring)
			{
				if (this.pathfindingService.FindPath(army, this.departmentOfTheInterior.Cities[0].WorldPosition, target.WorldPosition, PathfindingManager.RequestMode.Default, null, PathfindingFlags.IgnoreArmies | PathfindingFlags.IgnoreFogOfWar | PathfindingFlags.IgnoreSieges | PathfindingFlags.IgnoreTerraformDevices | PathfindingFlags.IgnoreKaijuGarrisons, null) == null)
				{
					return false;
				}
				return true;
			}
		}
		return true;
	}

	private bool IsFreeOfBloomsOrFriendly(PointOfInterest pointOfInterest)
	{
		return true;
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(1);
		base.WriteXml(writer);
		writer.WriteStartElement("QuestBTOrders");
		writer.WriteAttributeString<int>("Count", this.QuestBTOrders.Count);
		for (int i = 0; i < this.QuestBTOrders.Count; i++)
		{
			IXmlSerializable xmlSerializable = this.QuestBTOrders[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num > 0 && reader.IsStartElement("QuestBTOrders"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("QuestBTOrders");
			this.QuestBTOrders.Clear();
			for (int i = 0; i < attribute; i++)
			{
				AILayer_QuestBTController.QuestBTOrder questBTOrder = new AILayer_QuestBTController.QuestBTOrder("", GameEntityGUID.Zero, 1f);
				reader.ReadElementSerializable<AILayer_QuestBTController.QuestBTOrder>(ref questBTOrder);
				if (questBTOrder.IsValid())
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("ELCP {0}: Loading QuestBTOrder {1}", new object[]
						{
							base.AIEntity.Empire,
							questBTOrder.ToString()
						});
					}
					this.QuestBTOrders.Add(questBTOrder);
				}
			}
			reader.ReadEndElement("QuestBTOrders");
		}
	}

	public void AddQuestBTOrder(StaticString name, GameEntityGUID target, float strength)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} AddQuestBTOrder {1} {2} {3}", new object[]
			{
				base.AIEntity.Empire,
				name,
				target,
				strength
			});
		}
		this.QuestBTOrders.Add(new AILayer_QuestBTController.QuestBTOrder(name, target, strength));
	}

	public void RemoveQuestBTOrder(StaticString name, GameEntityGUID target)
	{
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} RemoveQuestBTOrder {1} {2}", new object[]
			{
				base.AIEntity.Empire,
				name,
				target
			});
		}
		this.QuestBTOrders.RemoveAll((AILayer_QuestBTController.QuestBTOrder BTo) => BTo.questName == name && BTo.objectiveGUID == target);
		foreach (AICommander aicommander in this.aILayer_ArmyManagement.AICommanders)
		{
			AICommander_QuestBTCommander aicommander_QuestBTCommander = aicommander as AICommander_QuestBTCommander;
			if (aicommander_QuestBTCommander != null && aicommander_QuestBTCommander.QuestName == name && aicommander_QuestBTCommander.SubObjectiveGuid == target)
			{
				aicommander_QuestBTCommander.ForceFinish = true;
			}
		}
	}

	public bool TryGetQuestBTOrder(StaticString name, GameEntityGUID target, out AILayer_QuestBTController.QuestBTOrder questBTOrder)
	{
		questBTOrder = this.QuestBTOrders.Find((AILayer_QuestBTController.QuestBTOrder BTo) => BTo.questName == name && BTo.objectiveGUID == target);
		return questBTOrder != null;
	}

	private GlobalObjectiveMessage CreateObjectiveFor(int regionIndex, IGameEntity pointOfInterest, StaticString questname)
	{
		GlobalObjectiveMessage globalObjectiveMessage = base.GenerateObjective(base.ObjectiveType, regionIndex, pointOfInterest.GUID);
		globalObjectiveMessage.ObjectiveState = questname;
		this.globalObjectiveMessages.Add(globalObjectiveMessage);
		return globalObjectiveMessage;
	}

	public void AddQuestBTOrder(StaticString name, GameEntityGUID target, float strength, string resource = null, int resourceAmount = 0)
	{
		AILayer_QuestBTController.QuestBTOrder questBTOrder = new AILayer_QuestBTController.QuestBTOrder(name, target, strength);
		if (!string.IsNullOrEmpty(resource))
		{
			questBTOrder.resourceNeeded = resource;
			questBTOrder.resourceNeededAmount = resourceAmount;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} AddQuestBTOrder {1}", new object[]
			{
				base.AIEntity.Empire,
				questBTOrder.ToString()
			});
		}
		this.QuestBTOrders.Add(questBTOrder);
	}

	public Dictionary<StaticString, float> ResourcesNeededForQuest
	{
		get
		{
			return this.resourcesNeededForQuest;
		}
	}

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfScience departmentOfScience;

	private IWorldPositionningService worldPositionningService;

	private IQuestManagementService questManagementService;

	private IQuestRepositoryService questRepositoryService;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IPathfindingService pathfindingService;

	private List<AILayer_QuestBTController.QuestBTOrder> QuestBTOrders;

	private AILayer_ArmyManagement aILayer_ArmyManagement;

	private Dictionary<ulong, float> messagePriorityOverrides;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private Dictionary<StaticString, float> resourcesNeededForQuest;

	public class QuestBTOrder : IXmlSerializable
	{
		public void WriteXml(XmlWriter writer)
		{
			writer.WriteVersionAttribute(3);
			writer.WriteElementString<StaticString>("questName", this.questName);
			writer.WriteElementString<ulong>("objectiveGUID", this.objectiveGUID);
			writer.WriteElementString<float>("requiredArmyPower", this.requiredArmyPower);
			writer.WriteElementString<StaticString>("resourceNeeded", this.resourceNeeded);
			writer.WriteElementString<int>("resourceNeededAmount", this.resourceNeededAmount);
		}

		public void ReadXml(XmlReader reader)
		{
			int num = reader.ReadVersionAttribute();
			reader.ReadStartElement("QuestBTOrder");
			this.questName = reader.ReadElementString("questName");
			this.objectiveGUID = reader.ReadElementString<ulong>("objectiveGUID");
			if (num > 1)
			{
				this.requiredArmyPower = reader.ReadElementString<float>("requiredArmyPower");
			}
			if (num > 2)
			{
				this.resourceNeeded = reader.ReadElementString("resourceNeeded");
				this.resourceNeededAmount = reader.ReadElementString<int>("resourceNeededAmount");
			}
			reader.ReadEndElement("QuestBTOrder");
		}

		public override string ToString()
		{
			string text = (this.questName != null) ? this.questName.ToString() : "null";
			string text2 = (this.resourceNeeded != StaticString.Empty) ? (", " + this.resourceNeeded.ToString() + ": " + this.resourceNeededAmount.ToString()) : "";
			return string.Format("{0}:{1},{2}{3}", new object[]
			{
				text,
				this.objectiveGUID,
				this.requiredArmyPower,
				text2
			});
		}

		public bool IsValid()
		{
			return this.questName != "" && this.objectiveGUID != GameEntityGUID.Zero;
		}

		public QuestBTOrder(StaticString questname, GameEntityGUID targetGUID, float armypower = 1f)
		{
			this.questName = questname;
			this.objectiveGUID = targetGUID;
			this.requiredArmyPower = armypower;
			this.resourceNeeded = StaticString.Empty;
		}

		public StaticString questName;

		public GameEntityGUID objectiveGUID;

		public float requiredArmyPower;

		public StaticString resourceNeeded;

		public int resourceNeededAmount;
	}
}
