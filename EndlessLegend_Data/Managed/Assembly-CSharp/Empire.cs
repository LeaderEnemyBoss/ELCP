using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Xml;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class Empire : Amplitude.Unity.Game.Empire, IXmlSerializable, IDescriptorEffectProvider, IPropertyEffectFeatureProvider
{
	public Empire(StaticString name) : base(name)
	{
		this.IsControlledByAI = true;
		this.PlayerControllers = new global::Empire.PlayerControllersContainer();
	}

	public Empire(StaticString name, Faction faction, int colorIndex) : this(name)
	{
		if (faction == null)
		{
			Diagnostics.LogError("Faction is null for empire '{0}'.", new object[]
			{
				name
			});
			throw new ArgumentNullException("faction");
		}
		this.Faction = faction;
		this.ColorIndex = colorIndex;
		this.IsControlledByAI = true;
	}

	protected Empire()
	{
		this.IsControlledByAI = true;
		this.PlayerControllers = new global::Empire.PlayerControllersContainer();
	}

	SimulationObject IPropertyEffectFeatureProvider.GetSimulationObject()
	{
		return base.SimulationObject;
	}

	public override void ReadXml(XmlReader reader)
	{
		string attribute = reader.GetAttribute("FactionDescriptor");
		this.Faction = Faction.Decode(attribute);
		IDatabase<Faction> database = Databases.GetDatabase<Faction>(false);
		Faction faction;
		if (database.TryGetValue(this.Faction.Name, out faction))
		{
			MinorFaction minorFaction = faction as MinorFaction;
			if (minorFaction != null)
			{
				this.Faction = (MinorFaction)minorFaction.Clone();
			}
			NavalFaction navalFaction = faction as NavalFaction;
			if (navalFaction != null)
			{
				this.Faction = (NavalFaction)navalFaction.Clone();
			}
		}
		this.ColorIndex = reader.GetAttribute<int>("ColorIndex");
		base.ReadXml(reader);
		IDatabase<SimulationDescriptor> database2 = Databases.GetDatabase<SimulationDescriptor>(true);
		List<FactionTrait> list = new List<FactionTrait>(Faction.EnumerableTraits(this.Faction));
		SimulationDescriptor simulationDescriptor;
		for (int i = 0; i < list.Count; i++)
		{
			FactionTrait factionTrait = list[i];
			if (factionTrait != null && factionTrait.SimulationDescriptorReferences != null)
			{
				for (int j = 0; j < factionTrait.SimulationDescriptorReferences.Length; j++)
				{
					if (database2.TryGetValue(factionTrait.SimulationDescriptorReferences[j], out simulationDescriptor) && !base.SimulationObject.DescriptorHolders.Exists((SimulationDescriptorHolder match) => match.Descriptor.Name == simulationDescriptor.Name))
					{
						base.SimulationObject.AddDescriptor(simulationDescriptor);
					}
				}
			}
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("FactionDescriptor", Faction.Encode(this.Faction));
		writer.WriteAttributeString<int>("ColorIndex", this.ColorIndex);
		base.WriteXml(writer);
	}

	public Color Color
	{
		get
		{
			return this.color;
		}
	}

	public StaticString DefaultClass
	{
		get
		{
			return "ClassEmpire";
		}
	}

	public Faction Faction { get; private set; }

	public bool IsControlledByAI { get; set; }

	public virtual string LocalizedName { get; set; }

	[Obsolete("Use Empire.PlayerControllers.Client|Server instead.")]
	public global::PlayerController PlayerController { get; set; }

	public global::Empire.PlayerControllersContainer PlayerControllers { get; set; }

	internal string PlayerID
	{
		get
		{
			return string.Format("empire#{0:D2}", base.Index);
		}
	}

	private int ColorIndex
	{
		get
		{
			return this.colorIndex;
		}
		set
		{
			if (this.colorIndex != value)
			{
				if (value < 0)
				{
					Diagnostics.LogError("Invalid color index; must be a positive integer.");
					return;
				}
				string value2 = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
				IDatabase<Palette> database = Databases.GetDatabase<Palette>(false);
				if (database == null)
				{
					Diagnostics.LogError("Unable to retrieve the color palette (name: '{0}').", new object[]
					{
						value2
					});
					return;
				}
				Palette palette;
				if (database.TryGetValue(value2, out palette))
				{
					if (palette.Colors == null || palette.Colors.Length == 0)
					{
						Diagnostics.LogError("Invalid color palette (name: '{0}').", new object[]
						{
							value2
						});
						return;
					}
					if (value >= palette.Colors.Length)
					{
						Diagnostics.LogError("Invalid color index; out of palette range (index: {0}, palette: '{1}', palette length: {2}).", new object[]
						{
							value,
							value2,
							palette.Colors.Length
						});
						return;
					}
					this.colorIndex = value;
					this.color = palette.Colors[value].Color;
				}
			}
		}
	}

	public IEnumerable<SimulationDescriptor> GetDescriptors()
	{
		foreach (SimulationDescriptorHolder holder in base.SimulationObject.DescriptorHolders)
		{
			if (!(holder.Descriptor.Name == "ClassEmpire"))
			{
				if (!(holder.Descriptor.Name == "EmpireTypeMajor"))
				{
					yield return holder.Descriptor;
				}
			}
		}
		yield break;
	}

	public override string ToString()
	{
		return string.Format("Empire {0}", base.Index);
	}

	public void Update<T>(GameInterface gameInterface, T gameState) where T : GameState
	{
		GameServerState_Turn_Main gameServerState_Turn_Main = gameState as GameServerState_Turn_Main;
		if (gameServerState_Turn_Main != null)
		{
			this.Update(gameInterface, gameServerState_Turn_Main);
		}
		foreach (Agency agency in base.Agencies)
		{
			if (agency is IGameStateUpdatable<T>)
			{
				(agency as IGameStateUpdatable<T>).Update(gameInterface);
			}
		}
	}

	public virtual void Update(GameInterface gameInterface, GameServerState_Turn_Main gameState)
	{
	}

	public IEnumerator LoadGame(global::Game game)
	{
		this.UpdateGameModifiers(game);
		yield return this.OnLoadGame(game);
		yield break;
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		DepartmentOfDefense agency = base.GetAgency<DepartmentOfDefense>();
		if (agency != null)
		{
			agency.OnEmpireEliminated(empire, authorized);
		}
		DepartmentOfEducation agency2 = base.GetAgency<DepartmentOfEducation>();
		if (agency2 != null)
		{
			agency2.OnEmpireEliminated(empire, authorized);
		}
		DepartmentOfTheInterior agency3 = base.GetAgency<DepartmentOfTheInterior>();
		if (agency3 != null)
		{
			agency3.OnEmpireEliminated(empire, authorized);
		}
	}

	protected void ApplyGameModifier(StaticString gameModifierReference, PlayerType playerType)
	{
		IDatabase<GameModifierDefinition> database = Databases.GetDatabase<GameModifierDefinition>(false);
		Diagnostics.Assert(database != null);
		GameModifierDefinition gameModifierDefinition;
		if (!database.TryGetValue(gameModifierReference, out gameModifierDefinition))
		{
			Diagnostics.LogError("Can't found game modifier {0} in database.", new object[]
			{
				gameModifierReference
			});
			return;
		}
		Diagnostics.Assert(gameModifierDefinition != null);
		if (gameModifierDefinition.DescriptorTypesToRemove != null)
		{
			for (int i = 0; i < gameModifierDefinition.DescriptorTypesToRemove.Length; i++)
			{
				string x = gameModifierDefinition.DescriptorTypesToRemove[i];
				base.RemoveDescriptorByType(x);
			}
		}
		if (gameModifierDefinition.EffectsList != null)
		{
			for (int j = 0; j < gameModifierDefinition.EffectsList.Length; j++)
			{
				GameModifierDefinition.Effects effects = gameModifierDefinition.EffectsList[j];
				Diagnostics.Assert(effects != null);
				if (effects.PlayerTypeFilter == PlayerType.Unset || effects.PlayerTypeFilter == playerType)
				{
					bool flag = true;
					if (effects.Prerequisites != null)
					{
						for (int k = 0; k < effects.Prerequisites.Length; k++)
						{
							flag &= effects.Prerequisites[k].Check(base.SimulationObject);
						}
					}
					if (flag)
					{
						if (effects.SimulationDescriptors != null)
						{
							for (int l = 0; l < effects.SimulationDescriptors.Length; l++)
							{
								SimulationDescriptor descriptor = effects.SimulationDescriptors[l];
								base.AddDescriptor(descriptor, false);
							}
						}
					}
				}
			}
		}
	}

	protected virtual void CreateAgencies()
	{
	}

	protected override void Dispose(bool disposing)
	{
		base.Dispose(disposing);
		this.PlayerController = null;
		this.PlayerControllers.Client = null;
		this.PlayerControllers.Server = null;
		this.Faction = null;
	}

	protected IEnumerator GameServerState_AI_Turn_Begin(string context, string pass)
	{
		yield return null;
		yield break;
	}

	protected override IEnumerator OnInitialize()
	{
		this.CreateAgencies();
		base.RegisterPass("GameServerState_Turn_Begin", "AI_Turn_Begin", new Agency.Action(this.GameServerState_AI_Turn_Begin), new string[0]);
		yield return base.OnInitialize();
		yield break;
	}

	protected void UpdateGameModifiers(global::Game game)
	{
		Diagnostics.Assert(game != null);
		PlayerType playerType = (!this.IsControlledByAI) ? PlayerType.Human : PlayerType.AI;
		this.Refresh(true);
		this.ApplyGameModifier(GameModifierDefinition.GetGameDifficultyReference(game.GameDifficulty), playerType);
		this.Refresh(true);
		this.ApplyGameModifier(GameModifierDefinition.GetGameSpeedReference(game.GameSpeed), playerType);
		this.Refresh(true);
		this.ApplyGameModifier(GameModifierDefinition.GetMinorFactionDifficultyReference(game.MinorFactionDifficulty), playerType);
		this.Refresh(true);
		DepartmentOfHealth agency = base.GetAgency<DepartmentOfHealth>();
		if (agency != null)
		{
			agency.RefreshApprovalStatus();
		}
	}

	protected void CheckNavalColorIndex()
	{
		string value = Amplitude.Unity.Framework.Application.Registry.GetValue<string>("Settings/UI/EmpireColorPalette", "Standard");
		IDatabase<Palette> database = Databases.GetDatabase<Palette>(false);
		Palette palette = null;
		if (database != null)
		{
			database.TryGetValue(value, out palette);
		}
		if (palette != null)
		{
			StaticString tag = "NavalEmpire";
			XmlColorReference xmlColorReference = palette.Colors.FirstOrDefault((XmlColorReference iterator) => iterator.Tags != null && iterator.Tags.Contains(tag));
			if (xmlColorReference != null)
			{
				this.ColorIndex = Array.IndexOf<XmlColorReference>(palette.Colors, xmlColorReference);
			}
		}
	}

	public static readonly StaticString TagEmpireEliminated = new StaticString("EmpireEliminated");

	private int colorIndex = -1;

	private Color color = Color.white;

	public class PlayerControllersContainer
	{
		public global::PlayerController Client { get; set; }

		public global::PlayerController Server { get; set; }

		public global::PlayerController AI
		{
			get
			{
				return this.Server;
			}
		}
	}
}
