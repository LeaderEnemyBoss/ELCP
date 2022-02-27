using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class WorldEffectManager : GameAncillary, IXmlSerializable, IService, IWorldEffectService
{
	List<float> IWorldEffectService.GetWorldPositionMovementCostModifiers(WorldPosition worldPosition)
	{
		if (this.positionMovementCostModifiers.ContainsKey(worldPosition))
		{
			return this.positionMovementCostModifiers[worldPosition];
		}
		return null;
	}

	void IWorldEffectService.AddWorldEffect(WorldEffectDefinition worldEffectDefinition, WorldPosition position, int empireIndex, GameEntityGUID ownerGUID, XmlReader reader)
	{
		WorldEffect worldEffect = Activator.CreateInstance(worldEffectDefinition.EffectType) as WorldEffect;
		worldEffect.Initialize(empireIndex, position, ownerGUID, worldEffectDefinition, this);
		List<WorldEffect> list;
		if (!this.worldEffects.ContainsKey(position))
		{
			list = new List<WorldEffect>();
			this.worldEffects.Add(position, list);
		}
		else
		{
			list = this.worldEffects[position];
		}
		list.Add(worldEffect);
		if (reader != null)
		{
			worldEffect.ReadXml(reader);
		}
		else
		{
			worldEffect.Activate();
		}
		this.WorldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(this.WorldPositionningService.GetRegionIndex(position));
	}

	void IWorldEffectService.RemoveWorldEffectFromPosition(WorldPosition position)
	{
		List<WorldEffect> list = null;
		try
		{
			list = this.worldEffects[position];
		}
		catch
		{
			Diagnostics.LogError("There isn't any effect at the position {0}", new object[]
			{
				position.ToString()
			});
			return;
		}
		for (int i = 0; i < list.Count; i++)
		{
			list[i].Deactivate();
		}
		list.Clear();
		this.worldEffects.Remove(position);
		this.WorldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(this.WorldPositionningService.GetRegionIndex(position));
	}

	void IWorldEffectService.RemoveWorldEffectFromOwnerGUID(GameEntityGUID ownerGUID)
	{
		List<WorldEffect> list = new List<WorldEffect>();
		foreach (List<WorldEffect> list2 in this.worldEffects.Values)
		{
			for (int i = 0; i < list2.Count; i++)
			{
				if (list2[i].OwnerGUID == ownerGUID)
				{
					list.Add(list2[i]);
				}
			}
		}
		int j = 0;
		while (j < list.Count)
		{
			WorldPosition worldPosition = list[j].WorldPosition;
			list[j].Deactivate();
			List<WorldEffect> list3 = null;
			try
			{
				list3 = this.worldEffects[worldPosition];
			}
			catch
			{
				Diagnostics.LogError("There isn't any effect at the position {0}", new object[]
				{
					worldPosition.ToString()
				});
				goto IL_11E;
			}
			goto IL_DC;
			IL_11E:
			j++;
			continue;
			IL_DC:
			list3.Remove(list[j]);
			if (list3.Count == 0)
			{
				this.worldEffects.Remove(worldPosition);
			}
			this.WorldPositionSimulationEvaluatorService.SetSomethingChangedOnRegion(this.WorldPositionningService.GetRegionIndex(worldPosition));
			goto IL_11E;
		}
	}

	ICollection<SimulationDescriptor> IWorldEffectService.GetFidsModifierDescriptors(WorldPosition position)
	{
		List<SimulationDescriptor> list = new List<SimulationDescriptor>();
		foreach (List<WorldEffect> list2 in this.worldEffects.Values)
		{
			for (int i = 0; i < list2.Count; i++)
			{
				WorldEffect_FIDSModifier worldEffect_FIDSModifier = list2[i] as WorldEffect_FIDSModifier;
				if (worldEffect_FIDSModifier != null && worldEffect_FIDSModifier.HasAnEffectOnPosition(position))
				{
					list.AddRange(worldEffect_FIDSModifier.GetFidsModifierDescriptorsHavingAnEffectOnPosition(position));
				}
			}
		}
		return list;
	}

	void IWorldEffectService.AddFidsModifierDescriptors(SimulationObject district, WorldPosition position, bool districtIsProxy)
	{
		foreach (List<WorldEffect> list in this.worldEffects.Values)
		{
			for (int i = 0; i < list.Count; i++)
			{
				WorldEffect_FIDSModifier worldEffect_FIDSModifier = list[i] as WorldEffect_FIDSModifier;
				if (worldEffect_FIDSModifier != null && worldEffect_FIDSModifier.HasAnEffectOnPosition(position))
				{
					worldEffect_FIDSModifier.AddFidsModifierDescriptors(district, position, districtIsProxy);
				}
			}
		}
	}

	public virtual void ReadXml(XmlReader reader)
	{
		IDatabase<WorldEffectDefinition> database = Databases.GetDatabase<WorldEffectDefinition>(false);
		this.TurnWhenLastBegun = reader.GetAttribute<int>("TurnWhenLastBegun");
		reader.ReadStartElement();
		int attribute = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("WorldEffects");
		for (int i = 0; i < attribute; i++)
		{
			int attribute2 = reader.GetAttribute<int>("EmpireIndex");
			GameEntityGUID ownerGUID = reader.GetAttribute<ulong>("OwnerGUID");
			WorldPosition position;
			position.Row = reader.GetAttribute<short>("Row");
			position.Column = reader.GetAttribute<short>("Column");
			string attribute3 = reader.GetAttribute("WorldEffectDefinitionName");
			reader.ReadStartElement("WorldEffect");
			WorldEffectDefinition worldEffectDefinition;
			if (database.TryGetValue(attribute3, out worldEffectDefinition))
			{
				((IWorldEffectService)this).AddWorldEffect(worldEffectDefinition, position, attribute2, ownerGUID, reader);
			}
			reader.ReadEndElement("WorldEffect");
		}
		reader.ReadEndElement("WorldEffects");
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		writer.WriteStartElement("WorldEffects");
		int num = 0;
		foreach (List<WorldEffect> list in this.worldEffects.Values)
		{
			num += list.Count;
		}
		writer.WriteAttributeString<int>("Count", num);
		foreach (List<WorldEffect> list2 in this.worldEffects.Values)
		{
			foreach (WorldEffect worldEffect in list2)
			{
				writer.WriteStartElement("WorldEffect");
				writer.WriteAttributeString<int>("EmpireIndex", worldEffect.Empire.Index);
				writer.WriteAttributeString<ulong>("OwnerGUID", worldEffect.OwnerGUID);
				writer.WriteAttributeString<short>("Row", worldEffect.WorldPosition.Row);
				writer.WriteAttributeString<short>("Column", worldEffect.WorldPosition.Column);
				writer.WriteAttributeString("WorldEffectDefinitionName", worldEffect.WorldEffectDefinition.XmlSerializableName);
				worldEffect.WriteXml(writer);
				writer.WriteEndElement();
			}
		}
		writer.WriteEndElement();
	}

	public IGameEntityRepositoryService GameEntityRepositoryService { get; private set; }

	public IWorldPositionSimulationEvaluatorService WorldPositionSimulationEvaluatorService { get; private set; }

	public IWorldPositionningService WorldPositionningService { get; private set; }

	public Dictionary<WorldPosition, List<float>> PositionMovementCostModifiers
	{
		get
		{
			return this.positionMovementCostModifiers;
		}
	}

	private int TurnWhenLastBegun { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		yield return base.BindService<IGameEntityRepositoryService>(serviceContainer, delegate(IGameEntityRepositoryService service)
		{
			this.GameEntityRepositoryService = service;
		});
		yield return base.BindService<IWorldPositionSimulationEvaluatorService>(serviceContainer, delegate(IWorldPositionSimulationEvaluatorService service)
		{
			this.WorldPositionSimulationEvaluatorService = service;
		});
		yield return base.BindService<IWorldPositionningService>(serviceContainer, delegate(IWorldPositionningService service)
		{
			this.WorldPositionningService = service;
		});
		serviceContainer.AddService<IWorldEffectService>(this);
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		yield break;
	}

	public override IEnumerator LoadGame(Game game)
	{
		yield return base.LoadGame(game);
		yield break;
	}

	public void OnBeginTurn()
	{
		if (base.Game.Turn <= this.TurnWhenLastBegun)
		{
			return;
		}
		this.TurnWhenLastBegun = base.Game.Turn;
		foreach (List<WorldEffect> list in this.worldEffects.Values)
		{
			for (int i = 0; i < list.Count; i++)
			{
				list[i].OnBeginTurn();
			}
		}
	}

	protected override void Releasing()
	{
		base.Releasing();
		this.worldEffects.Clear();
		this.positionMovementCostModifiers.Clear();
		this.WorldPositionningService = null;
		this.WorldPositionSimulationEvaluatorService = null;
		this.GameEntityRepositoryService = null;
	}

	private Dictionary<WorldPosition, List<WorldEffect>> worldEffects = new Dictionary<WorldPosition, List<WorldEffect>>();

	private Dictionary<WorldPosition, List<float>> positionMovementCostModifiers = new Dictionary<WorldPosition, List<float>>();
}
