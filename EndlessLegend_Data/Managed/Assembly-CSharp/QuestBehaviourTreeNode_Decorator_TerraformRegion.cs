using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_TerraformRegion : QuestBehaviourTreeNode_Decorator<EventEmpireWorldTerraformed>
{
	[XmlAttribute]
	public string Output_RegionTerraformedVarName { get; set; }

	[XmlAttribute]
	public bool RegionTerraformed { get; set; }

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.Output_RegionTerraformedVarName))
		{
			Diagnostics.LogError("Failed to specify tile counter variable name");
		}
		this.RegionTerraformed = false;
		base.UpdateQuestVariable(questBehaviour, this.Output_RegionTerraformedVarName, this.RegionTerraformed);
		return base.Initialize(questBehaviour);
	}

	protected override State Execute(QuestBehaviour questBehaviour, EventEmpireWorldTerraformed e, params object[] parameters)
	{
		if (base.CheckAgainstQuestInitiatorFilter(questBehaviour, e.TerraformingEmpire as global::Empire, base.QuestInitiatorFilter) && e.TerraformedTiles.Length > 0)
		{
			IGameService service = Services.GetService<IGameService>();
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			for (int i = 0; i < e.TerraformedTiles.Length; i++)
			{
				Region region = service2.GetRegion(e.TerraformedTiles[i]);
				bool flag = true;
				for (int j = 0; j < region.WorldPositions.Length; j++)
				{
					if (!service2.IsWaterTile(region.WorldPositions[j]) && !service2.HasRidge(region.WorldPositions[j]) && !service2.ContainsTerrainTag(region.WorldPositions[j], "TerrainTagVolcanic"))
					{
						flag = false;
						break;
					}
				}
				if (flag)
				{
					this.RegionTerraformed = flag;
					base.UpdateQuestVariable(questBehaviour, this.Output_RegionTerraformedVarName, this.RegionTerraformed);
					return State.Success;
				}
			}
		}
		return State.Running;
	}
}
