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
		if (!e.Reversible && base.CheckAgainstQuestInitiatorFilter(questBehaviour, e.TerraformingEmpire as global::Empire, base.QuestInitiatorFilter) && e.TerraformedTiles.Length != 0)
		{
			IGameService service = Services.GetService<IGameService>();
			World world = (service.Game as global::Game).World;
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			Diagnostics.Assert(service2 != null);
			for (int i = 0; i < e.TerraformedTiles.Length; i++)
			{
				QuestBehaviourTreeNode_Decorator_TerraformRegion.<>c__DisplayClass0_0 CS$<>8__locals1 = new QuestBehaviourTreeNode_Decorator_TerraformRegion.<>c__DisplayClass0_0();
				CS$<>8__locals1.region = service2.GetRegion(e.TerraformedTiles[i]);
				bool flag = true;
				int k;
				int j;
				for (j = 0; j < CS$<>8__locals1.region.WorldPositions.Length; j = k + 1)
				{
					if (!service2.IsWaterTile(CS$<>8__locals1.region.WorldPositions[j]) && !service2.HasRidge(CS$<>8__locals1.region.WorldPositions[j]) && (!service2.ContainsTerrainTag(CS$<>8__locals1.region.WorldPositions[j], "TerrainTagVolcanic") || world.TemporaryTerraformations.Exists((World.TemporaryTerraformation tt) => tt.worldPosition == CS$<>8__locals1.region.WorldPositions[j])))
					{
						flag = false;
						break;
					}
					k = j;
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
