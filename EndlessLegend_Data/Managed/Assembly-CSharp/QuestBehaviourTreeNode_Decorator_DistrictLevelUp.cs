using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_DistrictLevelUp : QuestBehaviourTreeNode_Decorator<EventDistrictLevelUp>
{
	public QuestBehaviourTreeNode_Decorator_DistrictLevelUp()
	{
		this.Level = 0;
		this.Output_DistrictSimObjectVarName = string.Empty;
		this.CityGUID = GameEntityGUID.Zero;
		this.DistrictGUID = GameEntityGUID.Zero;
		this.TargetCityVarName = string.Empty;
		this.Output_CityVarName = string.Empty;
	}

	[XmlAttribute]
	public string Output_CityVarName { get; set; }

	[XmlAttribute]
	public string TargetCityVarName { get; set; }

	[XmlAttribute]
	public int Level { get; set; }

	[XmlAttribute]
	public string Output_DistrictSimObjectVarName { get; set; }

	[XmlElement]
	public ulong DistrictGUID { get; set; }

	[XmlElement]
	public ulong CityGUID { get; set; }

	[Ancillary]
	protected IGameEntityRepositoryService GameEntityRepositoryService { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventDistrictLevelUp e, params object[] parameters)
	{
		Diagnostics.Assert(e.District != null);
		if (e.OldLevel >= this.Level || e.NewLevel < this.Level)
		{
			return State.Running;
		}
		if (e.City == null)
		{
			if (!(this.CityGUID == GameEntityGUID.Zero))
			{
				return State.Running;
			}
			return State.Success;
		}
		else
		{
			this.AddDistrictToQuestVariable(questBehaviour, e);
			if (this.CityGUID != GameEntityGUID.Zero && this.CityGUID != e.City.GUID)
			{
				return State.Running;
			}
			base.UpdateQuestVariable(questBehaviour, this.Output_CityVarName, e.City);
			if (!string.IsNullOrEmpty(this.Output_CityVarName) && base.CheckConditions(questBehaviour, e, parameters) == State.Success)
			{
				this.CityGUID = e.City.GUID;
			}
			return State.Success;
		}
	}

	protected void AddDistrictToQuestVariable(QuestBehaviour questBehaviour, EventDistrictLevelUp e)
	{
		if (e.District != null)
		{
			base.UpdateQuestVariable(questBehaviour, this.Output_DistrictSimObjectVarName, e.District);
			this.DistrictGUID = e.District.GUID;
		}
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Unable to retrieve the game service.");
			return false;
		}
		this.GameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Unable to retrieve the game entity repository service.");
			return false;
		}
		District @object;
		if (this.DistrictGUID != GameEntityGUID.Zero && !string.IsNullOrEmpty(this.Output_DistrictSimObjectVarName) && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_DistrictSimObjectVarName) && this.GameEntityRepositoryService.TryGetValue<District>(this.DistrictGUID, out @object))
		{
			QuestVariable questVariable = new QuestVariable(this.Output_DistrictSimObjectVarName);
			questVariable.Object = @object;
			questBehaviour.QuestVariables.Add(questVariable);
		}
		City object2;
		if (this.CityGUID != GameEntityGUID.Zero && !string.IsNullOrEmpty(this.Output_CityVarName) && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_CityVarName) && this.GameEntityRepositoryService.TryGetValue<City>(this.CityGUID, out object2))
		{
			QuestVariable questVariable2 = new QuestVariable(this.Output_CityVarName);
			questVariable2.Object = object2;
			questBehaviour.QuestVariables.Add(questVariable2);
		}
		City city = null;
		if (this.CityGUID == GameEntityGUID.Zero && !string.IsNullOrEmpty(this.TargetCityVarName) && questBehaviour.TryGetQuestVariableValueByName<City>(this.TargetCityVarName, out city) && city != null)
		{
			this.CityGUID = city.GUID;
		}
		return base.Initialize(questBehaviour);
	}
}
