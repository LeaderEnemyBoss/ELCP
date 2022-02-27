using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_AddQuestMarker : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_AddQuestMarker()
	{
		this.Tags = new Tags();
		this.RevealUnexploredLand = false;
		this.MarkerVisibleInFogOfWar = false;
		this.MarkerTypeName = QuestMarker.DefaultMarkerTypeName;
	}

	[XmlAttribute]
	public string Output_QuestMarkerVarName { get; set; }

	[XmlAttribute]
	public bool IgnoreInteraction { get; set; }

	[XmlElement]
	public ulong QuestMarkerGUID { get; set; }

	[XmlIgnore]
	public Tags Tags { get; set; }

	[XmlElement]
	public ulong TargetEntityGUID { get; set; }

	[XmlAttribute]
	public string TargetEntityVarName { get; set; }

	[XmlAttribute("Tags")]
	public string XmlSerializableTags
	{
		get
		{
			return this.Tags.ToString();
		}
		set
		{
			this.Tags = new Tags();
			this.Tags.ParseTags(value);
		}
	}

	[XmlAttribute]
	public bool RevealUnexploredLand { get; set; }

	[XmlAttribute]
	public bool MarkerVisibleInFogOfWar { get; set; }

	[XmlAttribute]
	public string MarkerTypeName { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		if (this.Tags.IsNullOrEmpty)
		{
			Diagnostics.LogWarning("Marker was added without any tags in quest {0} : abort.", new object[]
			{
				questBehaviour.Quest.QuestDefinition.XmlSerializableName
			});
			return State.Success;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null || !(service.Game is global::Game))
		{
			Diagnostics.LogError("Unable to retrieve the game service.");
			return State.Running;
		}
		if (!this.TryResolveTarget(questBehaviour))
		{
			return State.Running;
		}
		questBehaviour.Push(new QuestInstruction_UpdateQuestMarker
		{
			BoundTargetGUID = this.TargetEntityGUID,
			MarkerGUID = this.QuestMarkerGUID,
			Tags = this.Tags,
			Empire = questBehaviour.Initiator,
			RevealUnexploredLand = this.RevealUnexploredLand,
			MarkerVisibleInFogOfWar = this.MarkerVisibleInFogOfWar,
			MarkerTypeName = this.MarkerTypeName,
			IgnoreInteraction = this.IgnoreInteraction
		});
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
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
		if (!this.TryResolveTarget(questBehaviour))
		{
			return false;
		}
		if (this.QuestMarkerGUID == 0UL)
		{
			this.QuestMarkerGUID = service2.GenerateGUID();
		}
		QuestVariable questVariable = questBehaviour.GetQuestVariableByName(this.Output_QuestMarkerVarName);
		if (questVariable == null)
		{
			questVariable = new QuestVariable(this.Output_QuestMarkerVarName);
			questBehaviour.QuestVariables.Add(questVariable);
		}
		questVariable.Object = this.QuestMarkerGUID;
		return base.Initialize(questBehaviour);
	}

	private bool TryResolveTarget(QuestBehaviour questBehaviour)
	{
		if (this.TargetEntityGUID != 0UL)
		{
			return true;
		}
		QuestVariable questVariableByName = questBehaviour.GetQuestVariableByName(this.TargetEntityVarName);
		if (questVariableByName == null)
		{
			Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
			{
				this.TargetEntityVarName
			});
			return false;
		}
		if (questVariableByName.Object == null)
		{
			Diagnostics.LogError("Quest variable object is null (varname: '{0}')", new object[]
			{
				this.TargetEntityVarName
			});
			return false;
		}
		try
		{
			IGameEntity gameEntity;
			if (questVariableByName.Object is IEnumerable<object>)
			{
				gameEntity = ((questVariableByName.Object as IEnumerable<object>).ElementAt(0) as IGameEntity);
			}
			else
			{
				gameEntity = (questVariableByName.Object as IGameEntity);
			}
			if (gameEntity == null)
			{
				Diagnostics.LogWarning("Quest variable object is not a game entity (varname: '{0}')", new object[]
				{
					this.TargetEntityVarName
				});
			}
			else
			{
				this.TargetEntityGUID = gameEntity.GUID;
			}
		}
		catch
		{
			Diagnostics.LogError("Quest variable object is not a game entity (varname: '{0}')", new object[]
			{
				this.TargetEntityVarName
			});
			return false;
		}
		return true;
	}
}
