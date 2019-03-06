using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.View;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class QuestMarker : IXmlSerializable, IGameEntity, IGameEntityWithWorldPosition, IWorldPositionable, IWorldEntityMappingOverride
{
	public QuestMarker()
	{
		this.isVisibleInFogOfWar = true;
		this.MarkerTypeName = QuestMarker.DefaultMarkerTypeName;
	}

	bool IWorldEntityMappingOverride.TryResolve(out string mappingName)
	{
		mappingName = "WorldMarker";
		return true;
	}

	bool IWorldEntityMappingOverride.TryResolve(out InterpreterContext context)
	{
		SimulationObject simulationObject = null;
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity;
			if (service2 != null && service2.TryGetValue(this.BoundTargetGUID, out gameEntity))
			{
				SimulationObjectWrapper simulationObjectWrapper = gameEntity as SimulationObjectWrapper;
				if (simulationObjectWrapper != null)
				{
					simulationObject = simulationObjectWrapper.SimulationObject;
				}
			}
		}
		context = new InterpreterContext(simulationObject);
		context.Register("WorldMarkerType", this.MarkerTypeName);
		return true;
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.GUID = reader.GetAttribute<ulong>("GUID");
		this.BoundTargetGUID = reader.GetAttribute<ulong>("BoundTargetGUID");
		this.QuestGUID = reader.GetAttribute<ulong>("QuestGUID");
		this.MarkerTypeName = reader.GetAttribute("MarkerTypeName");
		this.IgnoreInteraction = reader.GetAttribute<bool>("IgnoreInteraction", false);
		string attribute = reader.GetAttribute("Tags");
		this.Tags = new Tags();
		this.Tags.ParseTags(attribute);
		reader.ReadStartElement();
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("GUID", this.GUID);
		writer.WriteAttributeString<ulong>("BoundTargetGUID", this.BoundTargetGUID);
		writer.WriteAttributeString<ulong>("QuestGUID", this.QuestGUID);
		writer.WriteAttributeString("MarkerTypeName", this.MarkerTypeName);
		writer.WriteAttributeString<bool>("IgnoreInteraction", this.IgnoreInteraction);
		writer.WriteAttributeString<Tags>("Tags", this.Tags);
	}

	public GameEntityGUID BoundTargetGUID { get; set; }

	public GameEntityGUID GUID { get; set; }

	public string MarkerTypeName { get; set; }

	public bool IgnoreInteraction { get; set; }

	public GameEntityGUID QuestGUID { get; set; }

	public Tags Tags { get; set; }

	public WorldPosition WorldPosition
	{
		get
		{
			IGameService service = Services.GetService<IGameService>();
			if (service != null)
			{
				IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
				IGameEntity gameEntity;
				if (service2 != null && service2.TryGetValue(this.BoundTargetGUID, out gameEntity))
				{
					IGameEntityWithWorldPosition gameEntityWithWorldPosition = gameEntity as IGameEntityWithWorldPosition;
					if (gameEntityWithWorldPosition != null)
					{
						return gameEntityWithWorldPosition.WorldPosition;
					}
				}
			}
			return WorldPosition.Invalid;
		}
	}

	public bool SetVisibilityInFogOfWar(bool visible)
	{
		GameObject gameObject = null;
		Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
		if (service == null)
		{
			Diagnostics.LogError("ViewService not found.");
			return false;
		}
		if (service.CurrentView == null)
		{
			Diagnostics.LogError("viewService.CurrentView is null.");
			return false;
		}
		WorldView worldView = service.FindByType(typeof(WorldView)) as WorldView;
		if (worldView == null)
		{
			Diagnostics.LogError("QuestMarker.SetVisibilityInFogOfWar: viewService.CurrentView is not a WorldView.");
			return false;
		}
		if (worldView.CurrentWorldViewTechnique == null)
		{
			Diagnostics.LogError("CurrentWorldViewTechnique is null.");
			return false;
		}
		IWorldEntityFactoryService service2 = worldView.CurrentWorldViewTechnique.Services.GetService<IWorldEntityFactoryService>();
		if (service2 == null)
		{
			Diagnostics.LogError("WorldEntityFactoryService not found.");
			return false;
		}
		if (!service2.TryGetValue(this.GUID, out gameObject))
		{
			Diagnostics.LogError("QuestMarker.SetVisibilityInFogOfWar: cannot find marker with guid='{0}'.", new object[]
			{
				this.GUID
			});
			return false;
		}
		if (gameObject == null)
		{
			Diagnostics.LogError("QuestMarker.SetVisibilityInFogOfWar: markerGameObject is null for marker with guid='{0}'.", new object[]
			{
				this.GUID
			});
			return false;
		}
		this.isVisibleInFogOfWar = visible;
		foreach (MeshRenderer meshRenderer in gameObject.GetComponentsInChildren<MeshRenderer>())
		{
			for (int j = 0; j < meshRenderer.materials.Length; j++)
			{
				float value = (!visible) ? 1f : 0f;
				meshRenderer.materials[j].SetFloat("_HideInFow", value);
			}
		}
		return true;
	}

	public bool IsVisibleFor(global::Empire empire)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null)
		{
			IVisibilityService service2 = service.Game.Services.GetService<IVisibilityService>();
			if (service != null)
			{
				if (this.isVisibleInFogOfWar)
				{
					return service2.IsWorldPositionExploredFor(this.WorldPosition, empire) || service2.IsWorldPositionVisibleFor(this.WorldPosition, empire);
				}
				return service2.IsWorldPositionVisibleFor(this.WorldPosition, empire);
			}
		}
		return false;
	}

	private const string MarkerHiddenInFogOfWarProperty = "_HideInFow";

	public static readonly StaticString DefaultMarkerTypeName = "QuestMarker";

	private bool isVisibleInFogOfWar;
}
