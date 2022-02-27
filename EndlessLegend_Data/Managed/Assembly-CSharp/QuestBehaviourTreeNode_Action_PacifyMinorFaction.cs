using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_PacifyMinorFaction : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_PacifyMinorFaction()
	{
		this.TargetEmpireIndex = -1;
	}

	[XmlAttribute("TargetEmpireVarName")]
	public string TargetEmpireVarName { get; set; }

	[XmlElement]
	public int TargetEmpireIndex { get; set; }

	public override void Release()
	{
		base.Release();
		this.game = null;
		this.playerControllerRepositoryService = null;
	}

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		global::Empire empire = this.game.Empires[this.TargetEmpireIndex];
		OrderPacifyMinorFaction orderPacifyMinorFaction = new OrderPacifyMinorFaction(questBehaviour.Initiator.Index, empire.Index, true);
		this.playerControllerRepositoryService.ActivePlayerController.PostOrder(orderPacifyMinorFaction);
		Diagnostics.Log("Posting order: {0}.", new object[]
		{
			orderPacifyMinorFaction.ToString()
		});
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.TargetEmpireIndex == -1)
		{
			global::Empire empire;
			if (!questBehaviour.TryGetQuestVariableValueByName<global::Empire>(this.TargetEmpireVarName, out empire))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.TargetEmpireVarName
				});
				return false;
			}
			if (empire == null)
			{
				Diagnostics.LogError("Quest variable object is null (varname: '{0}')", new object[]
				{
					this.TargetEmpireVarName
				});
				return false;
			}
			this.TargetEmpireIndex = empire.Index;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		this.playerControllerRepositoryService = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (this.playerControllerRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the player controller repository service.");
			return false;
		}
		this.game = (service.Game as global::Game);
		return base.Initialize(questBehaviour);
	}

	[XmlIgnore]
	protected IPlayerControllerRepositoryService playerControllerRepositoryService;

	[XmlIgnore]
	protected global::Game game;
}
