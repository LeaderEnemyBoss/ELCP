using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_ConditionCheck_RegionContainsEnemy : QuestBehaviourTreeNode_ConditionCheck
{
	public QuestBehaviourTreeNode_ConditionCheck_RegionContainsEnemy()
	{
		this.RegionIndex = -1;
		this.IgnoredEmpiresIndex = null;
	}

	[XmlAttribute("RegionVarName")]
	public string RegionVarName { get; set; }

	[XmlAttribute("IgnoredEmpiresVarName")]
	public string IgnoredEmpiresVarName { get; set; }

	[XmlElement]
	public int RegionIndex { get; set; }

	[XmlElement]
	public int[] IgnoredEmpiresIndex { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		if (this.RegionIndex != -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			if (game == null)
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
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			for (int i = 0; i < game.Empires.Length; i++)
			{
				if (i != empire.Index && (this.IgnoredEmpiresIndex == null || !this.IgnoredEmpiresIndex.Contains(i)))
				{
					global::Empire empire2 = game.Empires[i];
					bool flag;
					if (empire2 is MajorEmpire)
					{
						DiplomaticRelation diplomaticRelation = agency.GetDiplomaticRelation(empire2);
						Diagnostics.Assert(diplomaticRelation != null);
						Diagnostics.Assert(diplomaticRelation.State != null);
						flag = (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.War || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown);
					}
					else
					{
						flag = true;
					}
					if (flag)
					{
						DepartmentOfDefense agency2 = empire2.GetAgency<DepartmentOfDefense>();
						for (int j = 0; j < agency2.Armies.Count; j++)
						{
							if ((int)service2.GetRegionIndex(agency2.Armies[j].WorldPosition) == this.RegionIndex)
							{
								return State.Success;
							}
						}
					}
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
		IEnumerable<global::Empire> enumerable;
		if (this.IgnoredEmpiresIndex == null && questBehaviour.TryGetQuestVariableValueByName<global::Empire>(this.IgnoredEmpiresVarName, out enumerable))
		{
			int num = enumerable.Count<global::Empire>();
			this.IgnoredEmpiresIndex = new int[num];
			int num2 = 0;
			foreach (global::Empire empire in enumerable)
			{
				this.IgnoredEmpiresIndex[num2] = empire.Index;
				num2++;
			}
		}
		return base.Initialize(questBehaviour);
	}
}
