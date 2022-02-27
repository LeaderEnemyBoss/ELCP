using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_ConditionCheck_RegionContainsArmy : QuestBehaviourTreeNode_ConditionCheck
{
	public QuestBehaviourTreeNode_ConditionCheck_RegionContainsArmy()
	{
		this.RegionIndex = -1;
		this.ArmyGuid = GameEntityGUID.Zero;
		this.CheckAllArmies = true;
	}

	[XmlAttribute("RegionVarName")]
	public string RegionVarName { get; set; }

	[XmlAttribute("ArmyGuidVarName")]
	public string ArmyGuidVarName { get; set; }

	[XmlAttribute("CheckAllArmies")]
	private bool CheckAllArmies { get; set; }

	[XmlElement]
	public int RegionIndex { get; set; }

	[XmlElement]
	public GameEntityGUID ArmyGuid { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		this.Initialize(questBehaviour);
		if (this.RegionIndex != -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			if (service.Game as global::Game == null)
			{
				return State.Failure;
			}
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			global::Empire empire;
			if (gameEvent != null)
			{
				empire = (gameEvent.Empire as global::Empire);
			}
			else
			{
				empire = questBehaviour.Initiator;
			}
			DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
			if (this.CheckAllArmies)
			{
				for (int i = 0; i < agency.Armies.Count; i++)
				{
					if ((int)service2.GetRegionIndex(agency.Armies[i].WorldPosition) == this.RegionIndex)
					{
						return State.Success;
					}
				}
			}
			else
			{
				Army army = agency.GetArmy(this.ArmyGuid);
				if (army == null)
				{
					return State.Failure;
				}
				Region region = service2.GetRegion(army.WorldPosition);
				if (region == null)
				{
					return State.Failure;
				}
				if (this.RegionIndex == region.Index)
				{
					return State.Success;
				}
			}
		}
		return State.Failure;
	}

	public override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.RegionIndex == -1)
		{
			Region region;
			if (!questBehaviour.TryGetQuestVariableValueByName<Region>(this.RegionVarName, out region))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.RegionVarName
				});
				return false;
			}
			if (region == null)
			{
				Diagnostics.LogError("Region is null or empty, quest variable (varname: '{0}')", new object[]
				{
					this.RegionVarName
				});
				return false;
			}
			this.RegionIndex = region.Index;
		}
		ulong num;
		if (!this.CheckAllArmies && this.ArmyGuid == 0UL && !string.IsNullOrEmpty(this.ArmyGuidVarName) && questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGuidVarName, out num))
		{
			if (num == 0UL)
			{
				Diagnostics.LogError("QuestBehaviourTreeNode_ConditionCheck_IsArmyAlive : Army guid is invalid");
				return false;
			}
			this.ArmyGuid = new GameEntityGUID(num);
		}
		return base.Initialize(questBehaviour);
	}
}
