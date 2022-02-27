using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;

public class QuestBehaviourTreeNode_Action_SendAICommand : QuestBehaviourTreeNode_Action
{
	[XmlAttribute]
	public QuestBehaviourTreeNode_Action_SendAICommand.CommandType Type { get; set; }

	[XmlAttribute("TargetVarName")]
	public string TargetVarName { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		if (!this.TryInitializingTargets(questBehaviour, this.ForceUpdate))
		{
			Diagnostics.LogError("ELCP {0} {2} {1} QuestBehaviourTreeNode_Action_SendAICommand TryInitializingTargets failed!", new object[]
			{
				questBehaviour.Initiator,
				this.Type,
				questBehaviour.Quest.QuestDefinition.Name
			});
			return State.Success;
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("ELCP {0} QuestBehaviourTreeNode_Action_SendAICommand {2} {1}, Target {3}, RequiredMilitaryPower {4}, Cancel {5}, Region {6}, ForceArmyGUID {7}, {8}, {9}", new object[]
			{
				questBehaviour.Initiator,
				this.Type,
				questBehaviour.Quest.QuestDefinition.Name,
				this.TargetEntityGUID,
				this.RequiredMilitaryPower,
				this.CancelOrder,
				this.TargetRegionIndex,
				this.ForceArmyGUID,
				this.ResourceName,
				this.WantedAmount
			});
		}
		ISessionService service = Services.GetService<ISessionService>();
		if (service == null || service.Session == null || !service.Session.IsHosting)
		{
			return State.Success;
		}
		GameServer gameServer = (service.Session as global::Session).GameServer as GameServer;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (gameServer != null && gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(questBehaviour.Initiator as MajorEmpire, out aiplayer_MajorEmpire) && aiplayer_MajorEmpire.AIState != AIPlayer.PlayerState.EmpireControlledByHuman)
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				if (this.Type == QuestBehaviourTreeNode_Action_SendAICommand.CommandType.VisitTarget && this.TargetEntityGUID > 0UL)
				{
					if (this.ForceArmyGUID > 0UL)
					{
						AILayer_QuestSolver layer = entity.GetLayer<AILayer_QuestSolver>();
						if (!this.CancelOrder)
						{
							layer.AddQuestSolverOrder(questBehaviour.Quest.QuestDefinition.Name, new GameEntityGUID(this.TargetEntityGUID), new GameEntityGUID(this.ForceArmyGUID));
						}
						else
						{
							layer.RemoveQuestSolverOrder(questBehaviour.Quest.QuestDefinition.Name, new GameEntityGUID(this.ForceArmyGUID));
						}
					}
					else
					{
						AILayer_QuestBTController layer2 = entity.GetLayer<AILayer_QuestBTController>();
						if (!this.CancelOrder)
						{
							if (!string.IsNullOrEmpty(this.ResourceName))
							{
								layer2.AddQuestBTOrder(questBehaviour.Quest.QuestDefinition.Name, new GameEntityGUID(this.TargetEntityGUID), this.RequiredMilitaryPower, this.ResourceName, this.WantedAmount);
							}
							else
							{
								layer2.AddQuestBTOrder(questBehaviour.Quest.QuestDefinition.Name, new GameEntityGUID(this.TargetEntityGUID), this.RequiredMilitaryPower);
							}
						}
						else
						{
							layer2.RemoveQuestBTOrder(questBehaviour.Quest.QuestDefinition.Name, new GameEntityGUID(this.TargetEntityGUID));
						}
					}
				}
				else if (this.Type == QuestBehaviourTreeNode_Action_SendAICommand.CommandType.SuspendPacification)
				{
					AILayer_Village layer3 = entity.GetLayer<AILayer_Village>();
					if (!this.CancelOrder)
					{
						layer3.SuspendPacifications(questBehaviour.Quest.QuestDefinition.Name);
					}
					else
					{
						layer3.ResumePacifications(questBehaviour.Quest.QuestDefinition.Name);
					}
				}
				else if (this.Type == QuestBehaviourTreeNode_Action_SendAICommand.CommandType.PacifyVillage && this.TargetEntityGUID > 0UL)
				{
					AILayer_Village layer4 = entity.GetLayer<AILayer_Village>();
					if (!this.CancelOrder)
					{
						layer4.AddQuestVillageToPrioritize(questBehaviour.Quest.QuestDefinition.Name, this.TargetEntityGUID);
					}
					else
					{
						layer4.RemoveQuestVillageToPrioritize(questBehaviour.Quest.QuestDefinition.Name, this.TargetEntityGUID);
					}
				}
				else if (this.Type == QuestBehaviourTreeNode_Action_SendAICommand.CommandType.PacifyRegion && this.TargetRegionIndex >= 0)
				{
					AILayer_Pacification layer5 = entity.GetLayer<AILayer_Pacification>();
					if (!this.CancelOrder)
					{
						layer5.AddQuestBTPacification(questBehaviour.Quest.QuestDefinition.Name, this.TargetRegionIndex);
					}
					else
					{
						layer5.RemoveQuestBTPacification(questBehaviour.Quest.QuestDefinition.Name, this.TargetRegionIndex);
					}
				}
			}
		}
		return State.Success;
	}

	public QuestBehaviourTreeNode_Action_SendAICommand()
	{
		this.RequiredMilitaryPower = 1f;
		this.TargetRegionIndex = -1;
		this.ResourceName = string.Empty;
		this.WantedAmount = -1;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		string text;
		if (!string.IsNullOrEmpty(this.ResourceNameVarName) && string.IsNullOrEmpty(this.ResourceName) && questBehaviour.TryGetQuestVariableValueByName<string>(this.ResourceNameVarName, out text))
		{
			if (string.IsNullOrEmpty(text))
			{
				Diagnostics.LogError("Resource name is null or empty, quest variable (varname: '{0}')", new object[]
				{
					this.ResourceNameVarName
				});
			}
			this.ResourceName = text;
		}
		QuestRegisterVariable questRegisterVariable;
		if (!string.IsNullOrEmpty(this.WantedAmountVarName) && this.WantedAmount == -1 && questBehaviour.TryGetQuestVariableValueByName<QuestRegisterVariable>(this.WantedAmountVarName, out questRegisterVariable))
		{
			if (questRegisterVariable == null)
			{
				Diagnostics.LogError("QuestRegisterVariable is null, quest variable (varname: '{0}')", new object[]
				{
					this.WantedAmountVarName
				});
			}
			this.WantedAmount = questRegisterVariable.Value;
		}
		this.TryInitializingTargets(questBehaviour, false);
		return base.Initialize(questBehaviour);
	}

	[XmlElement]
	public ulong TargetEntityGUID { get; set; }

	[XmlAttribute("RequiredMilitaryPower")]
	public float RequiredMilitaryPower { get; set; }

	[XmlAttribute("CancelOrder")]
	public bool CancelOrder { get; set; }

	[XmlElement]
	public int TargetRegionIndex { get; set; }

	[XmlAttribute("TargetRegionVarName")]
	public string TargetRegionVarName { get; set; }

	[XmlAttribute("ForceArmyVarName")]
	public string ForceArmyVarName { get; set; }

	[XmlElement]
	public ulong ForceArmyGUID { get; set; }

	private bool TryInitializingTargets(QuestBehaviour questBehaviour, bool forceUpdate = false)
	{
		bool result = true;
		if ((this.TargetRegionIndex == -1 || forceUpdate) && !string.IsNullOrEmpty(this.TargetRegionVarName))
		{
			Region region = null;
			if (!questBehaviour.TryGetQuestVariableValueByName<Region>(this.TargetRegionVarName, out region))
			{
				Diagnostics.LogWarning("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.TargetRegionVarName
				});
				result = false;
			}
			else
			{
				this.TargetRegionIndex = region.Index;
			}
		}
		if ((this.TargetEntityGUID == GameEntityGUID.Zero || forceUpdate) && !string.IsNullOrEmpty(this.TargetVarName))
		{
			IGameEntity gameEntity = null;
			if (!questBehaviour.TryGetQuestVariableValueByName<IGameEntity>(this.TargetVarName, out gameEntity))
			{
				Diagnostics.LogWarning("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.TargetVarName
				});
				result = false;
			}
			else
			{
				this.TargetEntityGUID = gameEntity.GUID;
			}
		}
		if ((this.ForceArmyGUID == GameEntityGUID.Zero || forceUpdate) && !string.IsNullOrEmpty(this.ForceArmyVarName))
		{
			IGameEntity gameEntity2 = null;
			ulong forceArmyGUID = 0UL;
			GameEntityGUID zero = GameEntityGUID.Zero;
			if (questBehaviour.TryGetQuestVariableValueByName<IGameEntity>(this.ForceArmyVarName, out gameEntity2))
			{
				this.ForceArmyGUID = gameEntity2.GUID;
			}
			else if (questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ForceArmyVarName, out forceArmyGUID))
			{
				this.ForceArmyGUID = forceArmyGUID;
			}
			else if (questBehaviour.TryGetQuestVariableValueByName<GameEntityGUID>(this.ForceArmyVarName, out zero))
			{
				this.ForceArmyGUID = zero;
			}
			else
			{
				Diagnostics.LogWarning("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.ForceArmyVarName
				});
				result = false;
			}
		}
		return result;
	}

	[XmlAttribute]
	public bool ForceUpdate { get; set; }

	[XmlAttribute("ResourceNameVarName")]
	public string ResourceNameVarName { get; set; }

	[XmlAttribute("WantedAmountVarName")]
	public string WantedAmountVarName { get; set; }

	[XmlElement]
	public string ResourceName { get; set; }

	[XmlElement]
	public int WantedAmount { get; set; }

	public enum CommandType
	{
		SuspendPacification = 2,
		VisitTarget = 1,
		PacifyVillage = 3,
		PacifyRegion
	}
}
