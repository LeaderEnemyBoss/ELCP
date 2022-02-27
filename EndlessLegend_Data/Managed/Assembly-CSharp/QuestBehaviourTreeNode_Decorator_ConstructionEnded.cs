using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_ConstructionEnded : QuestBehaviourTreeNode_Decorator<EventConstructionEnded>
{
	public QuestBehaviourTreeNode_Decorator_ConstructionEnded()
	{
		this.ConstructionName = string.Empty;
		this.CityGUID = GameEntityGUID.Zero;
		this.TargetCityVarName = string.Empty;
		this.Output_CityVarName = string.Empty;
	}

	[XmlAttribute]
	public string Output_CityVarName { get; set; }

	[XmlAttribute]
	public string TargetCityVarName { get; set; }

	[XmlAttribute("ConstructionName")]
	public string ConstructionNameAttribute
	{
		get
		{
			return this.ConstructionName;
		}
		set
		{
			this.ConstructionName = value;
		}
	}

	[XmlAttribute("ConstructionNameVarName")]
	public string ConstructionNameVarName { get; set; }

	[XmlElement]
	public string ConstructionName { get; set; }

	[XmlElement]
	public ulong CityGUID { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventConstructionEnded e, params object[] parameters)
	{
		if (e.ConstructibleElement.Name == this.ConstructionName)
		{
			IGameService service = Services.GetService<IGameService>();
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			IGameEntity gameEntity = null;
			City city = null;
			if (service2.TryGetValue(e.Context, out gameEntity))
			{
				city = (gameEntity as City);
				if (city == null && this.TargetCityVarName != string.Empty)
				{
					return State.Running;
				}
			}
			if (city != null)
			{
				City city2;
				if (this.TargetCityVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<City>(this.TargetCityVarName, out city2) && city2.GUID != city.GUID)
				{
					return State.Running;
				}
				QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_CityVarName);
				if (questVariable == null)
				{
					questVariable = new QuestVariable(this.Output_CityVarName);
					questBehaviour.QuestVariables.Add(questVariable);
				}
				questVariable.Object = city;
				this.CityGUID = city.GUID;
			}
			return State.Success;
		}
		return State.Running;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.ConstructionName))
		{
			string text;
			if (!questBehaviour.TryGetQuestVariableValueByName<string>(this.ConstructionNameVarName, out text))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
				{
					this.ConstructionNameVarName
				});
				return false;
			}
			if (string.IsNullOrEmpty(text))
			{
				Diagnostics.LogError("Construction name is null or empty (varname: '{0}')", new object[]
				{
					this.ConstructionNameVarName
				});
				return false;
			}
			this.ConstructionName = text;
		}
		if (this.CityGUID != GameEntityGUID.Zero && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_CityVarName))
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null)
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
			IGameEntity gameEntity;
			if (!service2.TryGetValue(this.CityGUID, out gameEntity))
			{
				Diagnostics.LogError("Unable to retrieve the game entity (GUID='{0}') in the repository service.", new object[]
				{
					this.CityGUID
				});
				return false;
			}
			City city = gameEntity as City;
			if (city == null)
			{
				Diagnostics.LogError("Unable to cast the game entity (GUID='{0}') to 'City'.", new object[]
				{
					this.CityGUID
				});
				return false;
			}
			QuestVariable questVariable = new QuestVariable(this.Output_CityVarName);
			questVariable.Object = city;
			questBehaviour.QuestVariables.Add(questVariable);
		}
		return base.Initialize(questBehaviour);
	}
}
