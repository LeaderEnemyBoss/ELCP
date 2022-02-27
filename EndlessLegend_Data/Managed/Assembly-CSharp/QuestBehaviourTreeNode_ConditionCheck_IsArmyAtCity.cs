using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_ConditionCheck_IsArmyAtCity : QuestBehaviourTreeNode_ConditionCheck
{
	public QuestBehaviourTreeNode_ConditionCheck_IsArmyAtCity()
	{
		this.CityGUID = GameEntityGUID.Zero;
		this.ArmyGuid = GameEntityGUID.Zero;
	}

	[XmlAttribute("CityVarName")]
	public string CityVarName { get; set; }

	[XmlAttribute("ArmyGuidVarName")]
	public string ArmyGuidVarName { get; set; }

	[XmlElement]
	public ulong CityGUID { get; set; }

	[XmlElement]
	public GameEntityGUID ArmyGuid { get; set; }

	public override State CheckCondition(QuestBehaviour questBehaviour, GameEvent gameEvent, params object[] parameters)
	{
		this.Initialize(questBehaviour);
		this.RefreshVar(questBehaviour);
		if (this.CityGUID != GameEntityGUID.Zero)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game x = service.Game as global::Game;
			if (x == null)
			{
				return State.Failure;
			}
			IWorldPositionningService service2 = service.Game.Services.GetService<IWorldPositionningService>();
			global::Empire empire;
			if (gameEvent != null)
			{
				empire = (gameEvent.Empire as global::Empire);
			}
			else
			{
				empire = questBehaviour.Initiator;
			}
			DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			DepartmentOfDefense agency2 = empire.GetAgency<DepartmentOfDefense>();
			Army army = agency2.GetArmy(this.ArmyGuid);
			if (army == null)
			{
				return State.Failure;
			}
			District district = service2.GetDistrict(army.WorldPosition);
			if (district == null)
			{
				return State.Failure;
			}
			if (district.City.GUID == this.CityGUID)
			{
				return State.Success;
			}
		}
		return State.Failure;
	}

	private void RefreshVar(QuestBehaviour questBehaviour)
	{
		object obj;
		if (questBehaviour.TryGetQuestVariableValueByName<object>(this.ArmyGuidVarName, out obj))
		{
			ulong num;
			if (obj is ulong)
			{
				num = (ulong)obj;
			}
			else if (obj is GameEntityGUID)
			{
				num = (GameEntityGUID)obj;
			}
			else
			{
				num = 0UL;
			}
			if (num != 0UL && this.ArmyGuid != num)
			{
				this.ArmyGuid = num;
			}
		}
		if (questBehaviour.TryGetQuestVariableValueByName<object>(this.CityVarName, out obj))
		{
			if (obj is City)
			{
				this.CityGUID = ((City)obj).GUID;
			}
			else if (obj is ulong)
			{
				this.CityGUID = (ulong)obj;
			}
			else if (obj is GameEntityGUID)
			{
				this.CityGUID = (GameEntityGUID)obj;
			}
		}
	}

	public override bool Initialize(QuestBehaviour questBehaviour)
	{
		City city;
		if (this.CityGUID == GameEntityGUID.Zero && !string.IsNullOrEmpty(this.CityVarName) && questBehaviour.TryGetQuestVariableValueByName<City>(this.CityVarName, out city))
		{
			if (city == null)
			{
				Diagnostics.LogError("City is null or empty, quest variable (varname: '{0}')", new object[]
				{
					this.CityVarName
				});
				return false;
			}
			this.CityGUID = city.GUID;
		}
		ulong num;
		if (this.ArmyGuid == 0UL && !string.IsNullOrEmpty(this.ArmyGuidVarName) && questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGuidVarName, out num))
		{
			if (num == 0UL)
			{
				Diagnostics.LogError("QuestBehaviourTreeNode_ConditionCheck_IsArmyAtCity : Army guid is invalid");
				return false;
			}
			this.ArmyGuid = new GameEntityGUID(num);
		}
		return base.Initialize(questBehaviour);
	}
}
