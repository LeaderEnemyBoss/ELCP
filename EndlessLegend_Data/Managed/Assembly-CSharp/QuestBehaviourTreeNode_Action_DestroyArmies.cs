using System;
using System.Collections;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_DestroyArmies : QuestBehaviourTreeNode_Action
{
	[XmlAttribute("ArmyTag")]
	public string ArmyTag { get; set; }

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
		IEnumerator gameEntities = this.Game.Services.GetService<IGameEntityRepositoryService>().GameEntities;
		while (gameEntities.MoveNext())
		{
			object obj = gameEntities.Current;
			Army army = (obj as IGameEntity) as Army;
			if (army != null && army.HasTag(this.ArmyTag))
			{
				OrderDestroyArmy orderDestroyArmy = new OrderDestroyArmy(army.Empire.Index, army.GUID);
				Diagnostics.Log("Posting order: {0}.", new object[]
				{
					orderDestroyArmy.ToString()
				});
				empire2.PlayerControllers.Server.PostOrder(orderDestroyArmy);
			}
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
		if (string.IsNullOrEmpty(this.ArmyTag))
		{
			Diagnostics.LogError("Armies to be destroyed require theirs identifing tag");
			return false;
		}
		return base.Initialize(questBehaviour);
	}
}
