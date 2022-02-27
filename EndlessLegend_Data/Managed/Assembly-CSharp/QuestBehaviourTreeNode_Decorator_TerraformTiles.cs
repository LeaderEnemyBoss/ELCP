using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;

public class QuestBehaviourTreeNode_Decorator_TerraformTiles : QuestBehaviourTreeNode_Decorator<EventEmpireWorldTerraformed>
{
	[XmlAttribute]
	public string Output_NumberOfTilesVarName { get; set; }

	[XmlAttribute]
	public int NumberOfTiles { get; set; }

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.Output_NumberOfTilesVarName))
		{
			Diagnostics.LogError("Failed to specify tile counter variable name");
		}
		this.NumberOfTiles = (int)questBehaviour.Initiator.GetPropertyValue(SimulationProperties.TilesTerraformed);
		base.UpdateQuestVariable(questBehaviour, this.Output_NumberOfTilesVarName, this.NumberOfTiles);
		return base.Initialize(questBehaviour);
	}

	protected override State Execute(QuestBehaviour questBehaviour, EventEmpireWorldTerraformed e, params object[] parameters)
	{
		if (e.Reversible)
		{
			return State.Running;
		}
		if (string.IsNullOrEmpty(this.Output_NumberOfTilesVarName))
		{
			return State.Success;
		}
		if (base.CheckAgainstQuestInitiatorFilter(questBehaviour, e.TerraformingEmpire as Empire, base.QuestInitiatorFilter) && e.TerraformedTiles.Length != 0)
		{
			this.NumberOfTiles = (int)questBehaviour.Initiator.GetPropertyValue(SimulationProperties.TilesTerraformed);
			base.UpdateQuestVariable(questBehaviour, this.Output_NumberOfTilesVarName, this.NumberOfTiles);
			return State.Success;
		}
		return State.Running;
	}
}
