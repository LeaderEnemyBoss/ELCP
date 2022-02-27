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
								if (alert != null)
								{
									if (victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] == 0 || (victoryConditionStatus.LastTurnWhenAlertWasTriggered[m] < 0 && alert.Repeat))
									{
										bool flag = alert.Evaluate(this.InterpreterContext);
										if (flag)
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
				if (victoryCondition2.Alerts != null)
				{
					if (majorEmpire3.VictoryConditionStatuses != null)
					{
						MajorEmpire.VictoryConditionStatus victoryConditionStatus2;
						if (majorEmpire3.VictoryConditionStatuses.TryGetValue(victoryCondition2.Name, out victoryConditionStatus2))
						{
							for (int num8 = 0; num8 < victoryCondition2.Alerts.Length; num8++)
							{
								if (victoryConditionStatus2.LastTurnWhenAlertWasTriggered != null && num8 < victoryConditionStatus2.LastTurnWhenAlertWasTriggered.Length && victoryConditionStatus2.LastTurnWhenAlertWasTriggered[num8] >= base.Game.Turn)
								{
									if (victoryCondition2.Alerts[num8] != null)
									{
										EventVictoryConditionAlert eventToNotify = new EventVictoryConditionAlert(majorEmpire3, victoryCondition2, victoryConditionStatus2, num8);
										this.EventService.Notify(eventToNotify);
									}
								}
							}
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
					bool flag2 = victoryCondition2.Evaluate(new object[]
					{
						this.InterpreterContext
					});
					if (flag2)
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
		int num = writer.WriteVersionAttribute(2);
		if (num >= 2)
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
		ISessionService sessionService = Services.GetService<ISessionService>();
		this.InterpreterContext = new InterpreterContext(null);
		this.InterpreterContext.SimulationObject = new SimulationObject("VictoryController");
		this.InterpreterContext.SimulationObject.ModifierForward = ModifierForwardType.ParentOnly;
		IDatabase<SimulationDescriptor> simulationDescriptors = Databases.GetDatabase<SimulationDescriptor>(false);
		if (simulationDescriptors != null)
		{
			SimulationDescriptor descriptor;
			if (simulationDescriptors.TryGetValue("ClassVictoryController", out descriptor))
			{
				this.InterpreterContext.SimulationObject.AddDescriptor(descriptor);
			}
			string lobbyDataFilter = Amplitude.Unity.Runtime.Runtime.Registry.GetValue(VictoryManager.Registers.LobbyDataFilter);
			if (!string.IsNullOrEmpty(lobbyDataFilter))
			{
				string[] lobbyDataKeys = lobbyDataFilter.Split(Amplitude.String.Separators, StringSplitOptions.RemoveEmptyEntries);
				if (lobbyDataKeys.Length > 0 && sessionService != null && sessionService.Session != null)
				{
					foreach (string lobbyDataKey in lobbyDataKeys)
					{
						string lobbyDataValue = sessionService.Session.GetLobbyData<string>(lobbyDataKey, null);
						if (!string.IsNullOrEmpty(lobbyDataValue))
						{
							string descriptorName = string.Format("VictoryModifier{0}{1}", lobbyDataKey, lobbyDataValue);
							if (simulationDescriptors.TryGetValue(descriptorName, out descriptor))
							{
								this.InterpreterContext.SimulationObject.AddDescriptor(descriptor);
							}
						}
					}
				}
			}
			this.InterpreterContext.SimulationObject.Refresh();
		}
		int numberOfLandRegions = 0;
		if (base.Game.World.Regions != null)
		{
			for (int regionIndex = 0; regionIndex < base.Game.World.Regions.Length; regionIndex++)
			{
				if (!base.Game.World.Regions[regionIndex].IsOcean && !base.Game.World.Regions[regionIndex].IsWasteland)
				{
					numberOfLandRegions++;
				}
			}
		}
		if (numberOfLandRegions <= 0)
		{
			Diagnostics.LogError("Invalid number of regions (value: {0}), rounding up to '1'.", new object[]
			{
				numberOfLandRegions
			});
			numberOfLandRegions = 1;
		}
		Diagnostics.Log("Registering new data (name: {0}, value: {1})", new object[]
		{
			"NumberOfLandRegions",
			numberOfLandRegions
		});
		this.InterpreterContext.Register("NumberOfLandRegions", numberOfLandRegions);
		if (game.Turn == 0 && sessionService != null && sessionService.Session != null)
		{
			foreach (VictoryCondition victoryCondition in this.VictoryConditions)
			{
				if (sessionService.Session.GetLobbyData(victoryCondition.Name) != null && !sessionService.Session.GetLobbyData<bool>(victoryCondition.Name, false))
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
		}
		yield break;
	}

	internal void OnBeginTurn()
	{
		if (base.Game.Turn <= this.TurnWhenLastBegun)
		{
			Diagnostics.Log("Skipping interaction because quests have already been triggered (turn: {0}, last checked: {1}).", new object[]
			{
				base.Game.Turn,
				this.TurnWhenLastBegun
			});
			return;
		}
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
					break;
				}
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
	}

	protected void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		ISessionService service = Services.GetService<ISessionService>();
		bool isHosting = service.Session.IsHosting;
		if (e.RaisedEvent is EventSwapCity)
		{
			EventSwapCity eventSwapCity = e.RaisedEvent as EventSwapCity;
			if (isHosting)
			{
				this.CheckVictoryQuestForEmpire(base.Game.Empires[eventSwapCity.OldOwnerEmpireIndex]);
			}
		}
		else if (e.RaisedEvent is EventSettlerDied)
		{
			EventSettlerDied eventSettlerDied = e.RaisedEvent as EventSettlerDied;
			Empire empire = eventSettlerDied.Empire as Empire;
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

	public static readonly StaticString TagVictoryManager = new StaticString("VictoryManager");

	public static readonly StaticString TagVictoryManagerOnBeginTurn = new StaticString("VictoryManagerOnBeginTurn");

	private SimulationPath[] simulationPathNumberOfMainCities;

	public static class Registers
	{
		public static StaticString LobbyDataFilter = new StaticString("Gameplay/Ancillaries/VictoryManager/LobbyDataFilter");
	}
}
