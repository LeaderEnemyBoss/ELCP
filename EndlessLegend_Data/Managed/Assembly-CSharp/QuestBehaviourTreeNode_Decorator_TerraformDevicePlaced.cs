using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_TerraformDevicePlaced : QuestBehaviourTreeNode_Decorator<EventTerraformDeviceEntityCreated>
{
	public QuestBehaviourTreeNode_Decorator_TerraformDevicePlaced()
	{
		this.DevicePositionVarName = string.Empty;
	}

	[XmlAttribute("DevicePositionVarName")]
	public string DevicePositionVarName { get; set; }

	[XmlElement]
	public WorldPosition[] DevicePositions { get; set; }

	[XmlAttribute]
	public string Output_EntityVarName { get; set; }

	[XmlElement]
	public ulong EntityGUID { get; set; }

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		if (service == null || service.Game == null || !(service.Game is global::Game))
		{
			Diagnostics.LogError("Unable to retrieve the game service.");
			return false;
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		IEnumerable<WorldPosition> source;
		if (this.DevicePositions == null && questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.DevicePositionVarName, out source))
		{
			this.DevicePositions = source.ToArray<WorldPosition>();
		}
		if (this.EntityGUID == 0UL)
		{
			this.EntityGUID = service2.GenerateGUID();
		}
		QuestVariable questVariable = questBehaviour.GetQuestVariableByName(this.Output_EntityVarName);
		if (questVariable == null)
		{
			questVariable = new QuestVariable(this.Output_EntityVarName);
			questBehaviour.QuestVariables.Add(questVariable);
			IQuestManagementService service3 = game.Services.GetService<IQuestManagementService>();
			if (service3 != null)
			{
				QuestVariable questVariable2 = new QuestVariable(this.Output_EntityVarName, this.EntityGUID);
				service3.State.AddGlobalVariable(questBehaviour.Initiator.Index, questVariable2);
			}
		}
		questVariable.Object = this.EntityGUID;
		return base.Initialize(questBehaviour);
	}

	protected override State Execute(QuestBehaviour questBehaviour, EventTerraformDeviceEntityCreated e, params object[] parameters)
	{
		IGameService service = Services.GetService<IGameService>();
		global::Game game = service.Game as global::Game;
		if (this.DevicePositionVarName == string.Empty)
		{
			return State.Success;
		}
		IEnumerable<WorldPosition> source;
		if (this.DevicePositionVarName != null && questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.DevicePositionVarName, out source))
		{
			this.DevicePositions = source.ToArray<WorldPosition>();
		}
		if (this.DevicePositions != null && this.DevicePositions[0] == e.TerraformDevice.WorldPosition)
		{
			IQuestManagementService service2 = game.Services.GetService<IQuestManagementService>();
			if (service2 != null)
			{
				this.EntityGUID = e.TerraformDevice.GUID;
				QuestVariable questVariable = new QuestVariable(this.Output_EntityVarName, e.TerraformDevice);
				service2.State.AddGlobalVariable(questBehaviour.Initiator.Index, questVariable);
			}
			return State.Success;
		}
		return State.Running;
	}

	private void addQuestMarker(QuestBehaviour questBehaviour, TerraformDevice army)
	{
		QuestInstruction_UpdateQuestMarker questInstruction_UpdateQuestMarker = new QuestInstruction_UpdateQuestMarker();
		questInstruction_UpdateQuestMarker.BoundTargetGUID = army.GUID;
		questInstruction_UpdateQuestMarker.MarkerGUID = this.EntityGUID;
		questInstruction_UpdateQuestMarker.Tags = new Tags();
		questInstruction_UpdateQuestMarker.Tags.AddTag("Ruins");
		questInstruction_UpdateQuestMarker.MarkerTypeName = QuestMarker.DefaultMarkerTypeName;
		questInstruction_UpdateQuestMarker.Empire = questBehaviour.Initiator;
		questInstruction_UpdateQuestMarker.RevealUnexploredLand = true;
		questInstruction_UpdateQuestMarker.MarkerVisibleInFogOfWar = true;
		questInstruction_UpdateQuestMarker.IgnoreInteraction = false;
		questBehaviour.Push(questInstruction_UpdateQuestMarker);
	}
}
