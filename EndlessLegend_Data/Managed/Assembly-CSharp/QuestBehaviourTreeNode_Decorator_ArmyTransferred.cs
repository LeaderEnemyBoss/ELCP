using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_ArmyTransferred : QuestBehaviourTreeNode_Decorator<EventArmyTransferred>
{
	public QuestBehaviourTreeNode_Decorator_ArmyTransferred()
	{
		this.DestinationType = QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.Any;
		this.DestinationGUID = string.Empty;
		this.OtherThanDestination = false;
	}

	[XmlAttribute]
	public string ArmyGUID { get; set; }

	[XmlAttribute]
	public QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption DestinationType { get; set; }

	[XmlAttribute]
	public string DestinationGUID { get; set; }

	[XmlAttribute]
	public bool OtherThanDestination { get; set; }

	[XmlAttribute]
	public string Output_DestinationGUID { get; set; }

	[XmlElement]
	public ulong StoredDestination { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventArmyTransferred e, params object[] parameters)
	{
		bool flag;
		switch (this.DestinationType)
		{
		case QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.Army:
			flag = e.IsDestinationArmy;
			break;
		case QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.Camp:
			flag = e.IsDestinationCamp;
			break;
		case QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.City:
			flag = e.IsDestinationCity;
			break;
		case QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.Village:
			flag = e.IsDestinationVillage;
			break;
		default:
			flag = true;
			break;
		}
		if (!flag)
		{
			return State.Running;
		}
		bool flag2 = false;
		if (string.IsNullOrEmpty(this.ArmyGUID))
		{
			Diagnostics.LogError("The argument ArmyGUID is required.");
			return State.Failure;
		}
		object obj;
		if (!questBehaviour.TryGetQuestVariableValueByName<object>(this.ArmyGUID, out obj))
		{
			Diagnostics.LogError("Failed to retrieve the variable {0}.", new object[]
			{
				this.ArmyGUID
			});
			return State.Failure;
		}
		if (obj is GameEntityGUID)
		{
			flag2 = (e.Source == (GameEntityGUID)obj);
		}
		else if (obj is ulong)
		{
			flag2 = (e.Source == (ulong)obj);
		}
		if (!flag2)
		{
			return State.Running;
		}
		bool flag3 = false;
		if (string.IsNullOrEmpty(this.DestinationGUID))
		{
			flag3 = true;
		}
		else
		{
			object obj2;
			if (!questBehaviour.TryGetQuestVariableValueByName<object>(this.DestinationGUID, out obj2))
			{
				Diagnostics.LogError("Failed to retrieve the variable {0}.", new object[]
				{
					this.DestinationGUID
				});
				return State.Failure;
			}
			if (obj2 is GameEntityGUID)
			{
				flag3 = (e.Destination.GUID == (GameEntityGUID)obj2);
			}
			else if (obj2 is ulong)
			{
				flag3 = (e.Destination.GUID == (ulong)obj2);
			}
			else if (obj2 is IGarrison)
			{
				flag3 = (e.Destination.GUID == ((IGarrison)obj2).GUID);
			}
		}
		if (!((!this.OtherThanDestination) ? flag3 : (!flag3)))
		{
			return State.Running;
		}
		if (!string.IsNullOrEmpty(this.Output_DestinationGUID))
		{
			if (this.DestinationType != QuestBehaviourTreeNode_Decorator_ArmyTransferred.QuestTransferOption.Army)
			{
				Diagnostics.LogError("The argument Output_DestinationGUID is valid only when DestinationType is Army.");
				return State.Failure;
			}
			QuestVariable questVariable = questBehaviour.GetQuestVariableByName(this.Output_DestinationGUID);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.Output_DestinationGUID);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = e.Destination.GUID;
			this.StoredDestination = e.Destination.GUID;
		}
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.gameEntityRepositoryService == null)
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				Diagnostics.LogError("Failed to retrieve the game service.");
				return false;
			}
			global::Game game = service.Game as global::Game;
			if (game == null)
			{
				Diagnostics.LogError("Failed to cast gameService.Game to Game.");
				return false;
			}
			this.gameEntityRepositoryService = game.Services.GetService<IGameEntityRepositoryService>();
			if (this.gameEntityRepositoryService == null)
			{
				Diagnostics.LogError("Failed to retrieve the game entity repository service.");
				return false;
			}
		}
		IGarrison garrison;
		if (this.StoredDestination != 0UL && !string.IsNullOrEmpty(this.Output_DestinationGUID) && this.gameEntityRepositoryService.TryGetValue<IGarrison>(this.StoredDestination, out garrison))
		{
			base.UpdateQuestVariable(questBehaviour, this.Output_DestinationGUID, garrison.GUID);
		}
		return base.Initialize(questBehaviour);
	}

	[XmlIgnore]
	protected IGameEntityRepositoryService gameEntityRepositoryService;

	[Flags]
	public enum QuestTransferOption
	{
		Army = 0,
		Camp = 1,
		City = 2,
		Village = 3,
		Any = 4
	}
}
