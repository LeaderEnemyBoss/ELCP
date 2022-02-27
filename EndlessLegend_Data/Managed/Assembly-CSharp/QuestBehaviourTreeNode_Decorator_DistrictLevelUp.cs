using System;
using System.Linq;
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
		if (e.OldLevel < this.Level && e.NewLevel >= this.Level)
		{
			this.AddDistrictToQuestVariable(questBehaviour, e);
			if (e.City != null)
			{
				City city;
				if (this.TargetCityVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<City>(this.TargetCityVarName, out city) && city.GUID != e.City.GUID)
				{
					return State.Running;
				}
				QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_CityVarName);
				if (questVariable == null)
				{
					questVariable = new QuestVariable(this.Output_CityVarName);
					questBehaviour.QuestVariables.Add(questVariable);
				}
				questVariable.Object = e.City;
				this.CityGUID = e.City.GUID;
			}
			return State.Success;
		}
		return State.Running;
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
			Diagnostics.LogError("Failed to retrieve the game service.");
			return false;
		}
		this.GameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (this.GameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
			return false;
		}
		if (!string.IsNullOrEmpty(this.Output_DistrictSimObjectVarName) && this.DistrictGUID != 0UL)
		{
			IGameEntity gameEntity;
			if (!this.GameEntityRepositoryService.TryGetValue(this.DistrictGUID, out gameEntity))
			{
				Diagnostics.LogWarning("QuestBehaviourTreeNode_Decorator_DistrictLevelUp: district entity GUID is not valid (Variable name :'{0}')", new object[]
				{
					this.Output_DistrictSimObjectVarName
				});
				return false;
			}
			if (gameEntity != null)
			{
				QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_DistrictSimObjectVarName);
				if (questVariable == null)
				{
					questVariable = new QuestVariable(this.Output_DistrictSimObjectVarName);
					questBehaviour.QuestVariables.Add(questVariable);
				}
				questVariable.Object = gameEntity;
			}
		}
		if (this.CityGUID != GameEntityGUID.Zero && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_CityVarName))
		{
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			if (service2 == null)
			{
				Diagnostics.LogError("Unable to retrieve the game entity repository service.");
				return false;
			}
			IGameEntity gameEntity2;
			if (!service2.TryGetValue(this.CityGUID, out gameEntity2))
			{
				Diagnostics.LogError("Unable to retrieve the game entity (GUID='{0}') in the repository service.", new object[]
				{
					this.CityGUID
				});
				return false;
			}
			City city = gameEntity2 as City;
			if (city == null)
			{
				Diagnostics.LogError("Unable to cast the game entity (GUID='{0}') to 'City'.", new object[]
				{
					this.CityGUID
				});
				return false;
			}
			QuestVariable questVariable2 = new QuestVariable(this.Output_CityVarName);
			questVariable2.Object = city;
			questBehaviour.QuestVariables.Add(questVariable2);
		}
		return base.Initialize(questBehaviour);
	}
}
