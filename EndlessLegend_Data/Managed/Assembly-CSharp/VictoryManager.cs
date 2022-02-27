using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Path;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Simulation.SimulationModifierDescriptors;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class VictoryManager : GameAncillary, IXmlSerializable, IService, IVictoryManagementService
{
	public VictoryManager()
	{
		this.TurnWhenVictoryConditionsWereLastChecked = -1;
		this.VictoryConditionsRaisedThisTurn = new List<VictoryCondition>();
		this.VictoryConditionsDiscabledThisGame = new List<StaticString>();
		this.VictoryConditionsDiscardedThisGame = new List<StaticString>();
		this.simulationPathNumberOfMainCities = new SimulationPath[]
		{
			new SimulationPath("EmpireTypeMajor/MainCity"),
			new SimulationPath("EmpireTypeMajor/Garrison/MainCity")
		};
	}

	public event EventHandler<VictoryConditionRaisedEventArgs> VictoryConditionRaised;

	void IVictoryManagementService.CheckForAlerts(int turn)
	{
		if (turn < 0 || this.HasAlreadyWon)
		{
			return;
		}
		Diagnostics.Assert(this.InterpreterContext != null);
		Snapshot snapshot = null;
		IGameStatisticsManagementService service = Services.GetService<IGameStatisticsManagementService>();
		if (service != null && service.Snapshot != null)
		{
			string name = string.Format("Turn #{0}", turn);
			if (!service.Snapshot.TryGetSnapshot(name, out snapshot))
			{
				Diagnostics.LogWarning("Skipping check because snapshot is missing (turn: {0}).", new object[]
				{
					turn
				});
				return;
			}
			if (snapshot != null)
			{
				foreach (KeyValuePair<string, float> keyValuePair in snapshot.KeyValuePairs)
				{
					this.InterpreterContext.Register(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}
		Diagnostics.Assert(base.Game != null);
		this.InterpreterContext.Register("Turn", base.Game.Turn);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < base.Game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = base.Game.Empires[i] as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			num++;
			num3++;
			if (majorEmpire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
			{
				num2++;
				num3--;
			}
			else
			{
				for (int j = 0; j < this.simulationPathNumberOfMainCities.Length; j++)
				{
					num4 += (int)this.simulationPathNumberOfMainCities[j].CountValidatedObjects(majorEmpire);
				}
				DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
				if (agency != null)
				{
					num5 += agency.Cities.Count;
				}
			}
		}
		this.InterpreterContext.Register("NumberOfMajorEmpires", num);
		this.InterpreterContext.Register("NumberOfMajorEmpiresEliminated", num2);
		this.InterpreterContext.Register("NumberOfMajorEmpiresLeft", num3);
		this.InterpreterContext.Register("NumberOfMainCitiesLeft", num4);
		this.InterpreterContext.Register("NumberOfCitiesLeft", num5);
		for (int k = 0; k < base.Game.Empires.Length; k++)
		{
			MajorEmpire majorEmpire2 = base.Game.Empires[k] as MajorEmpire;
			if (majorEmpire2 == null)
			{
				break;
			}
			if (snapshot != null)
			{
				Snapshot snapshot2 = snapshot.TakeSnapshot(majorEmpire2.Name);
				if (snapshot2 != null)
				{
					foreach (KeyValuePair<string, float> keyValuePair2 in snapshot2.KeyValuePairs)
					{
						this.InterpreterContext.Register(keyValuePair2.Key, keyValuePair2.Value);
					}
				}
			}
			try
			{
				int num6 = 0;
				for (int l = 0; l < this.simulationPathNumberOfMainCities.Length; l++)
				{
					num6 += (int)this.simulationPathNumberOfMainCities[l].CountValidatedObjects(majorEmpire2);
				}
				this.InterpreterContext.Register("NumberOfMainCities", num6);
				this.InterpreterContext.Register("VictoryWonderProgress", this.GetWonderProgress(majorEmpire2));
				majorEmpire2.SimulationObject.AddChild(this.InterpreterContext.SimulationObject);
				foreach (VictoryCondition victoryCondition in this.VictoryConditionsFilteredThisGame)
				{
					if (majorEmpire2.VictoryConditionStatuses != null)
					{
						MajorEmpire.VictoryConditionStatus victoryConditionStatus;
						if (!majorEmpire2.VictoryConditionStatuses.TryGetValue(victoryCondition.Name, out victoryConditionStatus))
						{
							victoryConditionStatus = new MajorEmpire.VictoryConditionStatus();
							majorEmpire2.VictoryConditionStatuses.Add(victoryCondition.Name, victoryConditionStatus);
						}
						if (victoryCondition.Alerts != null)
						{
							if (victoryConditionStatus.LastTurnWhenAlertWasTriggered == null || victoryConditionStatus.LastTurnWhenAlertWasTriggered.Length != victoryCondition.Alerts.Length)
							{
								victoryConditionStatus.LastTurnWhenAlertWasTriggered = new int[victoryCondition.Alerts.Length];
							}
							for (int m = 0; m < victoryCondition.Alerts.Length; m++)
							{
								VictoryCondition.Alert alert = victoryCondition.Alerts[m];
								if (alert != null && (victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] == 0 || (victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] < 0 && alert.Repeat)))
								{
									if (alert.Evaluate(this.InterpreterContext))
									{
										victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] = base.Game.Turn;
									}
									else if (victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] > 0)
									{
										victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] = -victoryConditionStatus.LastTurnWhenAlertWasTriggered[m];
									}
								}
							}
						}
						if (victoryCondition.Progression != null)
						{
							if (victoryConditionStatus.Variables == null || victoryConditionStatus.Variables.Length != victoryCondition.Progression.Vars.Length)
							{
								victoryConditionStatus.Variables = new float[victoryCondition.Progression.Vars.Length];
							}
							for (int n = 0; n < victoryCondition.Progression.Vars.Length; n++)
							{
								victoryConditionStatus.Variables[n] = victoryCondition.Progression.Vars[n].Evaluate(this.InterpreterContext);
							}
						}
					}
				}
			}
			catch
			{
			}
			finally
			{
				majorEmpire2.SimulationObject.RemoveChild(this.InterpreterContext.SimulationObject);
			}
		}
		for (int num7 = 0; num7 < base.Game.Empires.Length; num7++)
		{
			MajorEmpire majorEmpire3 = base.Game.Empires[num7] as MajorEmpire;
			if (majorEmpire3 == null)
			{
				break;
			}
			foreach (VictoryCondition victoryCondition2 in this.VictoryConditionsFilteredThisGame)
			{
				MajorEmpire.VictoryConditionStatus victoryConditionStatus2;
				if (victoryCondition2.Alerts != null && majorEmpire3.VictoryConditionStatuses != null && majorEmpire3.VictoryConditionStatuses.TryGetValue(victoryCondition2.Name, out victoryConditionStatus2))
				{
					for (int num8 = 0; num8 < victoryCondition2.Alerts.Length; num8++)
					{
						if (victoryConditionStatus2.LastTurnWhenAlertWasTriggered != null && num8 < victoryConditionStatus2.LastTurnWhenAlertWasTriggered.Length && victoryConditionStatus2.LastTurnWhenAlertWasTriggered[num8] >= base.Game.Turn && victoryCondition2.Alerts[num8] != null)
						{
							EventVictoryConditionAlert eventToNotify = new EventVictoryConditionAlert(majorEmpire3, victoryCondition2, victoryConditionStatus2, num8);
							this.EventService.Notify(eventToNotify);
						}
					}
				}
			}
		}
	}

	void IVictoryManagementService.CheckForVictoryConditions(int turn)
	{
		if (turn <= this.TurnWhenVictoryConditionsWereLastChecked)
		{
			Diagnostics.Log("Skipping check because victory conditions have already been already checked for (turn: {0}, last checked: {1}).", new object[]
			{
				turn,
				this.TurnWhenVictoryConditionsWereLastChecked
			});
			return;
		}
		this.TurnWhenVictoryConditionsWereLastChecked = turn;
		this.VictoryConditionsRaisedThisTurn.Clear();
		Diagnostics.Assert(this.InterpreterContext != null);
		Snapshot snapshot = null;
		IGameStatisticsManagementService service = Services.GetService<IGameStatisticsManagementService>();
		if (service != null && service.Snapshot != null)
		{
			string name = string.Format("Turn #{0}", turn);
			if (!service.Snapshot.TryGetSnapshot(name, out snapshot))
			{
				Diagnostics.LogWarning("Skipping check because snapshot is missing (turn: {0}, last checked: {1}).", new object[]
				{
					turn,
					this.TurnWhenVictoryConditionsWereLastChecked
				});
				return;
			}
			if (snapshot != null)
			{
				foreach (KeyValuePair<string, float> keyValuePair in snapshot.KeyValuePairs)
				{
					this.InterpreterContext.Register(keyValuePair.Key, keyValuePair.Value);
				}
			}
		}
		Diagnostics.Assert(base.Game != null);
		this.InterpreterContext.Register("Turn", base.Game.Turn);
		int num = 0;
		int num2 = 0;
		int num3 = 0;
		int num4 = 0;
		int num5 = 0;
		for (int i = 0; i < base.Game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = base.Game.Empires[i] as MajorEmpire;
			if (majorEmpire == null)
			{
				break;
			}
			num++;
			num3++;
			if (majorEmpire.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
			{
				num2++;
				num3--;
			}
			else
			{
				for (int j = 0; j < this.simulationPathNumberOfMainCities.Length; j++)
				{
					num4 += (int)this.simulationPathNumberOfMainCities[j].CountValidatedObjects(majorEmpire);
				}
				DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
				if (agency != null)
				{
					num5 += agency.Cities.Count;
				}
			}
		}
		this.InterpreterContext.Register("NumberOfMajorEmpires", num);
		this.InterpreterContext.Register("NumberOfMajorEmpiresEliminated", num2);
		this.InterpreterContext.Register("NumberOfMajorEmpiresLeft", num3);
		this.InterpreterContext.Register("NumberOfMainCitiesLeft", num4);
		this.InterpreterContext.Register("NumberOfCitiesLeft", num5);
		for (int k = 0; k < base.Game.Empires.Length; k++)
		{
			MajorEmpire majorEmpire2 = base.Game.Empires[k] as MajorEmpire;
			if (majorEmpire2 == null)
			{
				break;
			}
			if (snapshot != null)
			{
				Snapshot snapshot2 = snapshot.TakeSnapshot(majorEmpire2.Name);
				if (snapshot2 != null)
				{
					foreach (KeyValuePair<string, float> keyValuePair2 in snapshot2.KeyValuePairs)
					{
						this.InterpreterContext.Register(keyValuePair2.Key, keyValuePair2.Value);
					}
				}
			}
			try
			{
				int num6 = 0;
				for (int l = 0; l < this.simulationPathNumberOfMainCities.Length; l++)
				{
					num6 += (int)this.simulationPathNumberOfMainCities[l].CountValidatedObjects(majorEmpire2);
				}
				this.InterpreterContext.Register("NumberOfMainCities", num6);
				majorEmpire2.SimulationObject.AddChild(this.InterpreterContext.SimulationObject);
				bool flag = false;
				VictoryCondition victoryCondition = null;
				foreach (VictoryCondition victoryCondition2 in this.VictoryConditionsFilteredThisGame)
				{
					if (victoryCondition2.Evaluate(new object[]
					{
						this.InterpreterContext
					}))
					{
						if (victoryCondition2.Name == "Shared")
						{
							victoryCondition = victoryCondition2;
						}
						else if (!majorEmpire2.SimulationObject.Tags.Contains(Empire.TagEmpireEliminated))
						{
							this.OnVictoryConditionRaised(new VictoryConditionRaisedEventArgs(victoryCondition2, majorEmpire2));
							flag = true;
						}
					}
				}
				if (victoryCondition != null && flag)
				{
					DepartmentOfForeignAffairs agency2 = majorEmpire2.GetAgency<DepartmentOfForeignAffairs>();
					if (agency2 != null && agency2.DiplomaticRelations != null)
					{
						foreach (Empire empire in base.Game.Empires)
						{
							if (empire != majorEmpire2)
							{
								DiplomaticRelation diplomaticRelation = agency2.GetDiplomaticRelation(empire);
								if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
								{
									this.OnVictoryConditionRaised(new VictoryConditionRaisedEventArgs(victoryCondition, empire));
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			finally
			{
				majorEmpire2.SimulationObject.RemoveChild(this.InterpreterContext.SimulationObject);
			}
		}
	}

	void IVictoryManagementService.DiscardAllVictoryConditionsRaisedThisTurn()
	{
		if (this.VictoryConditionsRaisedThisTurn != null && this.VictoryConditionsRaisedThisTurn.Count > 0)
		{
			for (int i = 0; i < this.VictoryConditionsRaisedThisTurn.Count; i++)
			{
				VictoryCondition victoryCondition = this.VictoryConditionsRaisedThisTurn[i];
				if (!this.VictoryConditionsDiscardedThisGame.Contains(victoryCondition.Name))
				{
					this.VictoryConditionsDiscardedThisGame.Add(victoryCondition.Name);
				}
			}
		}
	}

	public virtual void ReadXml(XmlReader reader)
	{
		this.TurnWhenVictoryConditionsWereLastChecked = reader.GetAttribute<int>("TurnWhenVictoryConditionsWereLastChecked");
		this.TurnWhenLastBegun = reader.GetAttribute<int>("TurnWhenLastBegun");
		int num = reader.ReadVersionAttribute();
		reader.ReadStartElement();
		if (num >= 2)
		{
			this.VictoryConditionsDiscardedThisGame.Clear();
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("VictoryConditionsDiscardedThisGame");
			for (int i = 0; i < attribute; i++)
			{
				string attribute2 = reader.GetAttribute("Name");
				reader.Skip("VictoryCondition");
				this.VictoryConditionsDiscardedThisGame.Add(attribute2);
			}
			reader.ReadEndElement("VictoryConditionsDiscardedThisGame");
		}
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString("AssemblyQualifiedName", base.GetType().AssemblyQualifiedName);
		writer.WriteAttributeString<int>("TurnWhenVictoryConditionsWereLastChecked", this.TurnWhenVictoryConditionsWereLastChecked);
		writer.WriteAttributeString<int>("TurnWhenLastBegun", this.TurnWhenLastBegun);
		if (writer.WriteVersionAttribute(2) >= 2)
		{
			writer.WriteStartElement("VictoryConditionsDiscardedThisGame");
			writer.WriteAttributeString<int>("Count", this.VictoryConditionsDiscardedThisGame.Count);
			for (int i = 0; i < this.VictoryConditionsDiscardedThisGame.Count; i++)
			{
				writer.WriteStartElement("VictoryCondition");
				writer.WriteAttributeString("Name", this.VictoryConditionsDiscardedThisGame[i].ToString());
				writer.WriteEndElement();
			}
			writer.WriteEndElement();
		}
	}

	public IDatabase<VictoryCondition> VictoryConditions { get; private set; }

	public IEnumerable<VictoryCondition> VictoryConditionsEnabledThisGame
	{
		get
		{
			foreach (VictoryCondition victoryCondition in this.VictoryConditions)
			{
				if (!this.VictoryConditionsDiscabledThisGame.Contains(victoryCondition.Name))
				{
					yield return victoryCondition;
				}
			}
			IEnumerator<VictoryCondition> enumerator = null;
			yield break;
			yield break;
		}
	}

	public IEnumerable<VictoryCondition> VictoryConditionsFilteredThisGame
	{
		get
		{
			foreach (VictoryCondition victoryCondition in this.VictoryConditions)
			{
				if (!this.VictoryConditionsDiscardedThisGame.Contains(victoryCondition.Name))
				{
					yield return victoryCondition;
				}
			}
			IEnumerator<VictoryCondition> enumerator = null;
			yield break;
			yield break;
		}
	}

	public bool HasAlreadyWon { get; set; }

	private IEventService EventService { get; set; }

	private InterpreterContext InterpreterContext { get; set; }

	private int TurnWhenLastBegun { get; set; }

	private int TurnWhenVictoryConditionsWereLastChecked { get; set; }

	private List<VictoryCondition> VictoryConditionsRaisedThisTurn { get; set; }

	private List<StaticString> VictoryConditionsDiscabledThisGame { get; set; }

	private List<StaticString> VictoryConditionsDiscardedThisGame { get; set; }

	public override IEnumerator BindServices(IServiceContainer serviceContainer)
	{
		yield return base.BindServices(serviceContainer);
		serviceContainer.AddService<IVictoryManagementService>(this);
		yield break;
	}

	public override IEnumerator Ignite(IServiceContainer serviceContainer)
	{
		yield return base.Ignite(serviceContainer);
		this.VictoryConditions = Databases.GetDatabase<VictoryCondition>(false);
		if (this.VictoryConditions == null)
		{
			Diagnostics.LogError("Failed to retrieve the database of victory definitions.");
		}
		this.EventService = Services.GetService<IEventService>();
		if (this.EventService != null)
		{
			this.EventService.EventRaise += this.EventService_EventRaise;
		}
		else
		{
			Diagnostics.LogError("Failed to retrieve the event service.");
		}
		yield break;
	}

	public override IEnumerator LoadGame(Game game)
	{
		yield return base.LoadGame(game);
		ISessionService service = Services.GetService<ISessionService>();
		this.InterpreterContext = new InterpreterContext(null);
		this.InterpreterContext.SimulationObject = new SimulationObject("VictoryController");
		this.InterpreterContext.SimulationObject.ModifierForward = ModifierForwardType.ParentOnly;
		float num = 1f;
		IDatabase<SimulationDescriptor> database = Databases.GetDatabase<SimulationDescriptor>(false);
		if (database != null)
		{
			SimulationDescriptor simulationDescriptor;
			if (database.TryGetValue("ClassVictoryController", out simulationDescriptor))
			{
				this.InterpreterContext.SimulationObject.AddDescriptor(simulationDescriptor);
			}
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue(VictoryManager.Registers.LobbyDataFilter);
			if (!string.IsNullOrEmpty(value))
			{
				string[] array = value.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				if (array.Length != 0 && service != null && service.Session != null)
				{
					foreach (string text in array)
					{
						string lobbyData = service.Session.GetLobbyData<string>(text, null);
						if (!string.IsNullOrEmpty(lobbyData))
						{
							string x = string.Format("VictoryModifier{0}{1}", text, lobbyData);
							if (database.TryGetValue(x, out simulationDescriptor))
							{
								this.InterpreterContext.SimulationObject.AddDescriptor(simulationDescriptor);
								SimulationModifierDescriptor simulationModifierDescriptor = Array.Find<SimulationModifierDescriptor>(simulationDescriptor.SimulationModifierDescriptors, (SimulationModifierDescriptor y) => y is SingleSimulationModifierDescriptor && y.TargetPropertyName == "PeacePointThreshold");
								if (simulationModifierDescriptor != null)
								{
									num *= float.Parse((simulationModifierDescriptor as SingleSimulationModifierDescriptor).Value);
								}
							}
						}
					}
				}
			}
			this.InterpreterContext.SimulationObject.Refresh();
		}
		foreach (Empire empire in base.Game.Empires)
		{
			if (empire is MajorEmpire)
			{
				(empire as MajorEmpire).SetPropertyBaseValue("TreatyPeacePointPerTurnMult", num);
			}
		}
		int num2 = 0;
		int num3 = 0;
		bool flag = Services.GetService<IDownloadableContentService>().IsShared(DownloadableContent16.ReadOnlyName);
		if (base.Game.World.Regions != null)
		{
			for (int j = 0; j < base.Game.World.Regions.Length; j++)
			{
				if (!base.Game.World.Regions[j].IsOcean && !base.Game.World.Regions[j].IsWasteland)
				{
					num2++;
				}
				else if (flag && base.Game.World.Regions[j].IsOcean && base.Game.World.Regions[j].NavalEmpire != null)
				{
					PirateCouncil agency = base.Game.World.Regions[j].NavalEmpire.GetAgency<PirateCouncil>();
					if (agency != null && agency.GetRegionFortresses(base.Game.World.Regions[j]).Count > 0)
					{
						num3++;
					}
				}
			}
		}
		if (num2 + num3 <= 0)
		{
			Diagnostics.LogError("Invalid number of regions (value: {0}), rounding up to '1'.", new object[]
			{
				num2
			});
			num2 = 1;
		}
		Diagnostics.Log("Registering new data (name: {0}, land: {1}, ocean: {2})", new object[]
		{
			"TotalNumberOfRegions",
			num2,
			num3
		});
		this.InterpreterContext.Register("NumberOfLandRegions", num2);
		this.InterpreterContext.Register("TotalNumberOfRegions", num2 + num3);
		if (game.Turn == 0 && service != null && service.Session != null)
		{
			using (IEnumerator<VictoryCondition> enumerator = this.VictoryConditions.GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					VictoryCondition victoryCondition = enumerator.Current;
					if (service.Session.GetLobbyData(victoryCondition.Name) != null && !service.Session.GetLobbyData<bool>(victoryCondition.Name, false))
					{
						if (!this.VictoryConditionsDiscabledThisGame.Contains(victoryCondition.Name))
						{
							this.VictoryConditionsDiscabledThisGame.Add(victoryCondition.Name);
							Diagnostics.Log("Disabling victory condition '{0}'.", new object[]
							{
								victoryCondition.Name
							});
						}
						if (!this.VictoryConditionsDiscardedThisGame.Contains(victoryCondition.Name))
						{
							this.VictoryConditionsDiscardedThisGame.Add(victoryCondition.Name);
							Diagnostics.Log("Discarding victory condition '{0}'.", new object[]
							{
								victoryCondition.Name
							});
						}
					}
				}
				yield break;
			}
		}
		yield break;
	}

	internal void OnBeginTurn()
	{
		if (base.Game.Turn > this.TurnWhenLastBegun || base.Game.Turn == 0)
		{
			this.TurnWhenLastBegun = base.Game.Turn;
			IQuestManagementService service = base.ServiceContainer.GetService<IQuestManagementService>();
			if (service != null)
			{
				service.State.Tags.Clear();
				service.State.Tags.AddTag(VictoryManager.TagVictoryManager);
				service.State.Tags.AddTag(VictoryManager.TagVictoryManagerOnBeginTurn);
				service.State.Tags.AddTag(QuestDefinition.TagExclusive);
				service.State.WorldPosition = WorldPosition.Invalid;
				for (int i = 0; i < base.Game.Empires.Length; i++)
				{
					if (!(base.Game.Empires[i] is MajorEmpire))
					{
						return;
					}
					if (base.Game.Turn != 0 || (base.Game.Empires[i] as MajorEmpire).IsSpectator)
					{
						Empire empire = base.Game.Empires[i];
						service.State.Empire = empire;
						service.State.Targets.Clear();
						service.State.AddTargets("$(Empire)", empire);
						service.State.AddTargets("$(Empires)", (from emp in base.Game.Empires
						where emp is MajorEmpire
						select emp).ToArray<Empire>());
						QuestDefinition questDefinition;
						QuestVariable[] questVariables;
						QuestInstruction[] pendingInstructions;
						QuestReward[] questRewards;
						Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
						if (service.TryTrigger(out questDefinition, out questVariables, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
						{
							service.Trigger(empire, questDefinition, questVariables, pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
						}
					}
				}
				return;
			}
		}
		else
		{
			Diagnostics.Log("Skipping interaction because quests have already been triggered (turn: {0}, last checked: {1}).", new object[]
			{
				base.Game.Turn,
				this.TurnWhenLastBegun
			});
		}
	}

	protected void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		bool isHosting = Services.GetService<ISessionService>().Session.IsHosting;
		if (e.RaisedEvent is EventSwapCity)
		{
			EventSwapCity eventSwapCity = e.RaisedEvent as EventSwapCity;
			if (isHosting)
			{
				this.CheckVictoryQuestForEmpire(base.Game.Empires[eventSwapCity.OldOwnerEmpireIndex]);
				return;
			}
		}
		else if (e.RaisedEvent is EventSettlerDied)
		{
			Empire empire = (e.RaisedEvent as EventSettlerDied).Empire as Empire;
			if (empire != null && empire is MajorEmpire && isHosting)
			{
				this.CheckVictoryQuestForEmpire(empire);
			}
		}
	}

	protected void CheckVictoryQuestForEmpire(Empire empire)
	{
		IQuestManagementService service = base.ServiceContainer.GetService<IQuestManagementService>();
		if (service != null)
		{
			service.State.Tags.Clear();
			service.State.Tags.AddTag(VictoryManager.TagVictoryManager);
			service.State.Tags.AddTag(VictoryManager.TagVictoryManagerOnBeginTurn);
			service.State.Tags.AddTag(QuestDefinition.TagExclusive);
			service.State.WorldPosition = WorldPosition.Invalid;
			service.State.Empire = empire;
			service.State.Targets.Clear();
			service.State.AddTargets("$(Empire)", empire);
			service.State.AddTargets("$(Empires)", (from emp in base.Game.Empires
			where emp is MajorEmpire
			select emp).ToArray<Empire>());
			QuestDefinition questDefinition;
			QuestVariable[] questVariables;
			QuestInstruction[] pendingInstructions;
			QuestReward[] questRewards;
			Dictionary<Region, List<string>> regionQuestLocalizationVariableDefinitionLocalizationKey;
			if (service.TryTrigger(out questDefinition, out questVariables, out pendingInstructions, out questRewards, out regionQuestLocalizationVariableDefinitionLocalizationKey))
			{
				service.Trigger(empire, questDefinition, questVariables, pendingInstructions, questRewards, regionQuestLocalizationVariableDefinitionLocalizationKey, null, true);
			}
		}
	}

	protected virtual void OnVictoryConditionRaised(VictoryConditionRaisedEventArgs e)
	{
		if (!this.VictoryConditionsRaisedThisTurn.Contains(e.VictoryCondition))
		{
			this.VictoryConditionsRaisedThisTurn.Add(e.VictoryCondition);
		}
		if (this.VictoryConditionRaised != null)
		{
			this.VictoryConditionRaised(this, e);
		}
	}

	protected override void Releasing()
	{
		if (this.InterpreterContext != null)
		{
			if (this.InterpreterContext.SimulationObject != null)
			{
				this.InterpreterContext.SimulationObject.Dispose();
				this.InterpreterContext.SimulationObject = null;
			}
			this.InterpreterContext = null;
		}
		this.VictoryConditions = null;
		if (this.EventService != null)
		{
			this.EventService.EventRaise -= this.EventService_EventRaise;
			this.EventService = null;
		}
		base.Releasing();
	}

	private float GetWonderProgress(MajorEmpire empire)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		DepartmentOfIndustry agency2 = empire.GetAgency<DepartmentOfIndustry>();
		float num = 0f;
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			Construction construction = agency2.GetConstructionQueue(agency.Cities[i]).Get((Construction x) => x.ConstructibleElement.SubCategory == "SubCategoryVictory");
			if (construction != null)
			{
				for (int j = 0; j < construction.CurrentConstructionStock.Length; j++)
				{
					if (construction.CurrentConstructionStock[j].PropertyName == "Production")
					{
						float stock = construction.CurrentConstructionStock[j].Stock;
						if (stock > 0f)
						{
							float num2 = stock / DepartmentOfTheTreasury.GetProductionCostWithBonus(agency.Cities[i], construction.ConstructibleElement, "Production");
							if (num2 > num)
							{
								num = num2;
							}
						}
					}
				}
			}
		}
		return num;
	}

	public static readonly StaticString TagVictoryManager = new StaticString("VictoryManager");

	public static readonly StaticString TagVictoryManagerOnBeginTurn = new StaticString("VictoryManagerOnBeginTurn");

	private SimulationPath[] simulationPathNumberOfMainCities;

	public static class Registers
	{
		public static StaticString LobbyDataFilter = new StaticString("Gameplay/Ancillaries/VictoryManager/LobbyDataFilter");
	}
}
