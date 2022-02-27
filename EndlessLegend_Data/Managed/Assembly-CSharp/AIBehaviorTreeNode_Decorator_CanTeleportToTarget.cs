using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_CanTeleportToTarget : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string TargetVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		global::Empire empire = aiBehaviorTree.AICommander.Empire;
		if (empire == null || !(empire is MajorEmpire))
		{
			aiBehaviorTree.ErrorCode = 10;
			return State.Failure;
		}
		if (!army.Empire.SimulationObject.Tags.Contains("FactionTraitAffinityStrategic") || !army.Empire.SimulationObject.Tags.Contains("BoosterTeleport") || army.HasSeafaringUnits() || army.GetPropertyValue(SimulationProperties.Movement) < 0.1f || army.IsSolitary || !ELCPUtilities.CheckCooldownPrerequisites(army))
		{
			return State.Failure;
		}
		if (string.IsNullOrEmpty(this.TargetVarName))
		{
			return State.Failure;
		}
		IGameEntity gameEntity = aiBehaviorTree.Variables[this.TargetVarName] as IGameEntity;
		if (!(gameEntity is City))
		{
			return State.Failure;
		}
		if (ELCPUtilities.CanTeleportToCity(gameEntity as City, army, this.worldPositionningService.GetRegion(army.WorldPosition), this.worldPositionningService, null))
		{
			return State.Success;
		}
		return State.Failure;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		this.encounterRepositoryService = service.Game.Services.GetService<IEncounterRepositoryService>();
		return base.Initialize(behaviourTree);
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.encounterRepositoryService = null;
	}

	private IWorldPositionningService worldPositionningService;

	private IEncounterRepositoryService encounterRepositoryService;
}
