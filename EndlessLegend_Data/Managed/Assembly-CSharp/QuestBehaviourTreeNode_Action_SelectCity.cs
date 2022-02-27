using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_SelectCity : QuestBehaviourTreeNode_Action
{
	[XmlAttribute]
	public string EmpireIndexVarName { get; set; }

	[XmlAttribute]
	public string Output_CityVarName { get; set; }

	[XmlElement]
	public ulong StoredCity { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		if (!string.IsNullOrEmpty(this.EmpireIndexVarName))
		{
			int num = -1;
			if (!questBehaviour.TryGetQuestVariableValueByName<int>(this.EmpireIndexVarName, out num))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.EmpireIndexVarName
				});
				return State.Failure;
			}
			if (num != questBehaviour.Initiator.Index)
			{
				return State.Success;
			}
			IGameService service = Services.GetService<IGameService>();
			if (service == null)
			{
				return State.Failure;
			}
			global::Game game = service.Game as global::Game;
			if (game == null)
			{
				return State.Failure;
			}
			DepartmentOfTheInterior agency = game.Empires[num].GetAgency<DepartmentOfTheInterior>();
			if (agency == null)
			{
				return State.Failure;
			}
			if (agency.Cities.Count == 0)
			{
				Diagnostics.LogError("Empire does not have cities");
				return State.Failure;
			}
			City city = agency.Cities[0];
			if (string.IsNullOrEmpty(this.Output_CityVarName))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.Output_CityVarName
				});
				return State.Failure;
			}
			this.UpdateQuestVariable(questBehaviour, this.Output_CityVarName, city);
			this.StoredCity = city.GUID;
		}
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		global::Game game = service.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("Failed to cast gameService.Game to Game.");
			return false;
		}
		this.gameEntityRepositoryService = game.Services.GetService<IGameEntityRepositoryService>();
		if (this.gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
			return false;
		}
		City value;
		if (this.StoredCity != 0UL && !string.IsNullOrEmpty(this.Output_CityVarName) && this.gameEntityRepositoryService.TryGetValue<City>(this.StoredCity, out value))
		{
			this.UpdateQuestVariable(questBehaviour, this.Output_CityVarName, value);
		}
		return base.Initialize(questBehaviour);
	}

	private void UpdateQuestVariable(QuestBehaviour questBehaviour, string name, object value)
	{
		if (!string.IsNullOrEmpty(name))
		{
			QuestVariable questVariable = questBehaviour.GetQuestVariableByName(name);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(name);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = value;
		}
	}

	[XmlIgnore]
	protected IGameEntityRepositoryService gameEntityRepositoryService;
}
