using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetAllEmpireTargets : AIBehaviorTreeNode_Decorator
{
	public AIBehaviorTreeNode_Decorator_GetAllEmpireTargets()
	{
		this.TargetEmpireVarName = string.Empty;
	}

	[XmlAttribute]
	public string Output_TargetListVarName { get; set; }

	[XmlAttribute]
	public string TargetEmpireVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		AIArmyMission.AIArmyMissionErrorCode armyUnlessLocked = base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army);
		if (armyUnlessLocked != AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		global::Empire empire = null;
		if (!(this.TargetEmpireVarName != string.Empty))
		{
			aiBehaviorTree.ErrorCode = 25;
			return State.Failure;
		}
		empire = (aiBehaviorTree.Variables[this.TargetEmpireVarName] as global::Empire);
		if (empire == null)
		{
			aiBehaviorTree.ErrorCode = 25;
			return State.Failure;
		}
		Diagnostics.Assert(empire != null);
		List<IWorldPositionable> list = new List<IWorldPositionable>();
		DepartmentOfDefense agency = empire.GetAgency<DepartmentOfDefense>();
		if (agency != null)
		{
			foreach (Army army2 in agency.Armies)
			{
				District district = this.worldPositionningService.GetDistrict(army2.WorldPosition);
				if (district == null || district.City.Empire != army2.Empire)
				{
					list.Add(army2);
				}
			}
		}
		DepartmentOfTheInterior agency2 = empire.GetAgency<DepartmentOfTheInterior>();
		if (agency2 != null)
		{
			foreach (City city in agency2.Cities)
			{
				list.Add(city.GetValidDistrictToTarget(army));
			}
		}
		if (list.Count != 0)
		{
			if (aiBehaviorTree.Variables.ContainsKey(this.Output_TargetListVarName))
			{
				aiBehaviorTree.Variables[this.Output_TargetListVarName] = list;
			}
			else
			{
				aiBehaviorTree.Variables.Add(this.Output_TargetListVarName, list);
			}
			return State.Success;
		}
		aiBehaviorTree.ErrorCode = 8;
		return State.Failure;
	}

	protected override bool Initialize(AIBehaviorTree questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		return base.Initialize(questBehaviour);
	}

	private IWorldPositionningService worldPositionningService;
}
