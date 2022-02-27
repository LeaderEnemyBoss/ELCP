using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_DestroyArmy : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_DestroyArmy()
	{
		this.ArmyGUID = 0UL;
	}

	[XmlAttribute("ArmyGUIDVarName")]
	public string ArmyGUIDVarName { get; set; }

	[XmlIgnore]
	private global::Game Game { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		global::Empire empire2 = this.Game.Empires.FirstOrDefault((global::Empire empire) => empire.Bits == questBehaviour.Quest.EmpireBits);
		if (empire2 == null)
		{
			Diagnostics.LogError("Failed to retrieve the (lesser) quest empire.");
			return State.Running;
		}
		ulong num;
		if (questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGUIDVarName, out num) && num != 0UL)
		{
			IGameEntityRepositoryService service = this.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity = null;
			if (!service.TryGetValue(num, out gameEntity))
			{
				Diagnostics.LogWarning("Action failed getting the targeted Army");
				return State.Success;
			}
			Army army = gameEntity as Army;
			if (army == null)
			{
				Diagnostics.LogError("Action failed getting the targeted Army");
				return State.Success;
			}
			OrderDestroyArmy orderDestroyArmy = new OrderDestroyArmy(army.Empire.Index, num);
			Diagnostics.Log("Posting order: {0}.", new object[]
			{
				orderDestroyArmy.ToString()
			});
			empire2.PlayerControllers.Server.PostOrder(orderDestroyArmy);
		}
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		this.Game = (service.Game as global::Game);
		if (this.Game == null)
		{
			Diagnostics.LogError("Failed to cast gameService.Game to Game.");
			return false;
		}
		if (this.ArmyGUID == 0UL)
		{
			ulong armyGUID;
			if (!string.IsNullOrEmpty(this.ArmyGUIDVarName) && questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGUIDVarName, out armyGUID))
			{
				this.ArmyGUID = armyGUID;
			}
		}
		else
		{
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.ArmyGUIDVarName);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.ArmyGUIDVarName);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = this.ArmyGUID;
		}
		return base.Initialize(questBehaviour);
	}

	[XmlElement]
	public ulong ArmyGUID { get; set; }
}
