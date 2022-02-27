using System;
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
		Diagnostics.Assert(e.ConstructibleElement != null);
		if (e.ConstructibleElement.Name != this.ConstructionName)
		{
			return State.Running;
		}
		City city;
		if (!Services.GetService<IGameService>().Game.Services.GetService<IGameEntityRepositoryService>().TryGetValue<City>(e.Context, out city))
		{
			if (!(this.CityGUID == GameEntityGUID.Zero))
			{
				return State.Running;
			}
			return State.Success;
		}
		else
		{
			if (this.CityGUID != GameEntityGUID.Zero && this.CityGUID != city.GUID)
			{
				return State.Running;
			}
			base.UpdateQuestVariable(questBehaviour, this.Output_CityVarName, city);
			if (!string.IsNullOrEmpty(this.Output_CityVarName))
			{
				this.CityGUID = city.GUID;
			}
			return State.Success;
		}
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.ConstructionName))
		{
			if (string.IsNullOrEmpty(this.ConstructionNameVarName))
			{
				Diagnostics.LogError("Missing attribute 'ConstructionNameVarName'");
				return false;
			}
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
		if (this.CityGUID != GameEntityGUID.Zero && !string.IsNullOrEmpty(this.Output_CityVarName) && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_CityVarName))
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
			City @object;
			if (service2.TryGetValue<City>(this.CityGUID, out @object))
			{
				QuestVariable questVariable = new QuestVariable(this.Output_CityVarName);
				questVariable.Object = @object;
				questBehaviour.QuestVariables.Add(questVariable);
			}
		}
		City city = null;
		if (this.CityGUID == GameEntityGUID.Zero && !string.IsNullOrEmpty(this.TargetCityVarName) && questBehaviour.TryGetQuestVariableValueByName<City>(this.TargetCityVarName, out city))
		{
			this.CityGUID = city.GUID;
		}
		return base.Initialize(questBehaviour);
	}
}
