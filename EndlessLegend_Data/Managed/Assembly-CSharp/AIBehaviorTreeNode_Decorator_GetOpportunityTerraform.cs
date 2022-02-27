using System;
using System.Xml.Serialization;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AIBehaviorTreeNode_Decorator_GetOpportunityTerraform : AIBehaviorTreeNode_Decorator
{
	[XmlAttribute]
	public string OpportunityTerraform_DestinationVarName { get; set; }

	protected override State Execute(AIBehaviorTree aiBehaviorTree, params object[] parameters)
	{
		if (aiBehaviorTree.AICommander.Empire.GetPropertyValue(SimulationProperties.LavapoolStock) < 1f)
		{
			return State.Failure;
		}
		Army army;
		if (base.GetArmyUnlessLocked(aiBehaviorTree, "$Army", out army) > AIArmyMission.AIArmyMissionErrorCode.None)
		{
			return State.Failure;
		}
		if (this.worldPositionningService.IsWaterTile(army.WorldPosition))
		{
			return State.Failure;
		}
		if (this.aILayer_Terraformation == null)
		{
			return State.Failure;
		}
		Region region = this.worldPositionningService.GetRegion(army.WorldPosition);
		if (region == null || !this.aILayer_Terraformation.OpportunityPositions.ContainsKey(region.Index) || region.Owner == null || region.Owner.Index != army.Empire.Index)
		{
			return State.Failure;
		}
		if (aiBehaviorTree.Variables.ContainsKey(this.OpportunityTerraform_DestinationVarName))
		{
			aiBehaviorTree.Variables[this.OpportunityTerraform_DestinationVarName] = this.aILayer_Terraformation.OpportunityPositions[region.Index];
		}
		else
		{
			aiBehaviorTree.Variables.Add(this.OpportunityTerraform_DestinationVarName, this.aILayer_Terraformation.OpportunityPositions[region.Index]);
		}
		return State.Success;
	}

	public override bool Initialize(BehaviourTree behaviourTree)
	{
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		if ((behaviourTree as AIBehaviorTree).AICommander.Empire is MajorEmpire)
		{
			AIEntity_Empire entity = (behaviourTree as AIBehaviorTree).AICommander.AIPlayer.GetEntity<AIEntity_Empire>();
			this.aILayer_Terraformation = entity.GetLayer<AILayer_Terraformation>();
		}
		return base.Initialize(behaviourTree);
	}

	public override void Release()
	{
		base.Release();
		this.worldPositionningService = null;
		this.aILayer_Terraformation = null;
	}

	private AILayer_Terraformation aILayer_Terraformation;

	private IWorldPositionningService worldPositionningService;
}
