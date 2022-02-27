using System;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class QuestBehaviourTreeNode_Action_UpdateArmyObjective : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_UpdateArmyObjective()
	{
		this.LesserEmpire = null;
		this.Objective = new QuestArmyObjective();
		this.ArmyGUID = GameEntityGUID.Zero;
		this.ArmyGUIDVarName = string.Empty;
		this.TargetCityGUIDVarName = string.Empty;
		this.TargetCityVarName = string.Empty;
		this.TargetEmpireIndexVarName = string.Empty;
		this.TargetEmpireVarName = string.Empty;
		this.SiegeTurnsNumberVarName = string.Empty;
		this.BehaviourName = string.Empty;
	}

	[XmlAttribute("ArmyGUIDVarName")]
	public string ArmyGUIDVarName { get; set; }

	[XmlAttribute("TargetCityVarName")]
	public string TargetCityVarName { get; set; }

	[XmlAttribute("TargetCityGUIDVarName")]
	public string TargetCityGUIDVarName { get; set; }

	[XmlAttribute("TargetEmpireVarName")]
	public string TargetEmpireVarName { get; set; }

	[XmlAttribute("TargetEmpireIndexVarName")]
	public string TargetEmpireIndexVarName { get; set; }

	[XmlAttribute("SiegeTurnsNumberVarName")]
	public string SiegeTurnsNumberVarName { get; set; }

	[XmlAttribute("TurnBeforeLeavingRegionVarName")]
	public string TurnBeforeLeavingRegionVarName { get; set; }

	[XmlAttribute("BehaviourName")]
	public string BehaviourName { get; set; }

	[XmlElement]
	public QuestArmyObjective Objective { get; set; }

	[XmlElement]
	public ulong ArmyGUID { get; set; }

	[XmlElement]
	public ulong CityGUID { get; set; }

	[XmlIgnore]
	private LesserEmpire LesserEmpire { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		this.ComputeObjective(questBehaviour);
		if (!this.Objective.IsValid)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective]  Execute: Invalid objective : {0}", new object[]
			{
				this.Objective.ToString()
			});
			this.PrintVariables(questBehaviour);
		}
		else
		{
			Diagnostics.Log("[UpdateArmyObjective] Execute: Valid objective : {0}", new object[]
			{
				this.Objective.ToString()
			});
		}
		this.ComputeArmyGUID(questBehaviour);
		if (this.ArmyGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] Execute: Invalid army GUID");
			Diagnostics.LogWarning("[UpdateArmyObjective] Execute: ArmyGUIDVarName={0}, GUID found: {1}", new object[]
			{
				this.ArmyGUIDVarName,
				this.ArmyGUID
			});
		}
		OrderUpdateArmyObjective order = new OrderUpdateArmyObjective(this.LesserEmpire.Index, this.ArmyGUID, this.Objective);
		this.LesserEmpire.PlayerControllers.Server.PostOrder(order);
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
		this.game = (service.Game as global::Game);
		if (this.game == null)
		{
			Diagnostics.LogError("Failed to cast gameService.Game to Game.");
			return false;
		}
		this.gameEntityRepositoryService = this.game.Services.GetService<IGameEntityRepositoryService>();
		if (this.gameEntityRepositoryService == null)
		{
			Diagnostics.LogError("Failed to retrieve the game entity repository service.");
			return false;
		}
		this.LesserEmpire = (LesserEmpire)this.game.Empires.FirstOrDefault((global::Empire match) => match is LesserEmpire);
		if (this.LesserEmpire == null)
		{
			Diagnostics.LogError("Failed to retrieve the (lesser) quest empire.");
			return false;
		}
		this.Objective = new QuestArmyObjective();
		this.Objective.BehaviourType = (QuestArmyObjective.QuestBehaviourType)((int)Enum.Parse(typeof(QuestArmyObjective.QuestBehaviourType), this.BehaviourName));
		this.ComputeArmyGUID(questBehaviour);
		if (this.ArmyGUID == GameEntityGUID.Zero)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] Initialize: Army GUID not valid yet");
		}
		this.ComputeObjective(questBehaviour);
		if (!this.Objective.IsValid)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] Initialize: Objective not valid yet");
		}
		return base.Initialize(questBehaviour);
	}

	private void ComputeArmyGUID(QuestBehaviour questBehaviour)
	{
		if (this.ArmyGUID == GameEntityGUID.Zero)
		{
			ulong num;
			if (questBehaviour.TryGetQuestVariableValueByName<ulong>(this.ArmyGUIDVarName, out num))
			{
				if (num == 0UL)
				{
					Diagnostics.LogWarning("[UpdateArmyObjective] Invalid Army GUID not found in quest variables, will try in Execute()");
				}
				else
				{
					this.ArmyGUID = num;
				}
			}
			else
			{
				Diagnostics.LogWarning("[UpdateArmyObjective] Invalid Army GUID not found in quest variables, will try in Execute()");
			}
		}
	}

	private void ComputeObjective(QuestBehaviour questBehaviour)
	{
		if (!string.IsNullOrEmpty(this.TargetCityGUIDVarName) || !string.IsNullOrEmpty(this.TargetCityVarName))
		{
			IGameEntity gameEntity;
			if (this.CityGUID == GameEntityGUID.Zero || !this.gameEntityRepositoryService.TryGetValue(this.CityGUID, out gameEntity) || !(gameEntity is City))
			{
				ulong num = GameEntityGUID.Zero;
				City city;
				if (this.TargetCityVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<City>(this.TargetCityVarName, out city))
				{
					this.CityGUID = city.GUID;
					this.Objective.TargetCityGUID = city.GUID;
					if (city.Empire != null)
					{
						this.Objective.TargetEmpireIndex = city.Empire.Index;
					}
				}
				else if (this.TargetCityGUIDVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<ulong>(this.TargetCityGUIDVarName, out num))
				{
					this.CityGUID = num;
					this.Objective.TargetCityGUID = num;
					IGameEntity gameEntity2;
					this.gameEntityRepositoryService.TryGetValue(num, out gameEntity2);
					if ((gameEntity2 as City).Empire != null)
					{
						this.Objective.TargetEmpireIndex = (gameEntity2 as City).Empire.Index;
					}
				}
				else if (this.CityGUID == GameEntityGUID.Zero || !this.gameEntityRepositoryService.TryGetValue(this.CityGUID, out gameEntity) || !(gameEntity is City))
				{
					Diagnostics.LogWarning("[UpdateArmyObjective] No city can be found in the quest variables ({0} {1})", new object[]
					{
						this.TargetCityVarName,
						this.TargetCityGUIDVarName
					});
					City city2 = null;
					if (questBehaviour.Initiator != null && this.Objective.BehaviourType == QuestArmyObjective.QuestBehaviourType.Offense)
					{
						DepartmentOfTheInterior agency = questBehaviour.Initiator.GetAgency<DepartmentOfTheInterior>();
						if (agency != null && agency.Cities != null && agency.Cities.Count > 0)
						{
							city2 = agency.Cities[UnityEngine.Random.Range(0, agency.Cities.Count - 1)];
						}
					}
					if (city2 == null)
					{
						this.Objective.BehaviourType = QuestArmyObjective.QuestBehaviourType.Roaming;
					}
					else
					{
						this.CityGUID = city2.GUID;
						this.Objective.TargetEmpireIndex = city2.Empire.Index;
					}
				}
			}
			this.Objective.TargetCityGUID = this.CityGUID;
		}
		if (this.Objective.TargetEmpireIndex == -1)
		{
			global::Empire empire = null;
			int targetEmpireIndex = -1;
			if (this.TargetEmpireVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<global::Empire>(this.TargetEmpireVarName, out empire))
			{
				this.Objective.TargetEmpireIndex = empire.Index;
			}
			else if (this.TargetEmpireIndexVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<int>(this.TargetEmpireIndexVarName, out targetEmpireIndex))
			{
				this.Objective.TargetEmpireIndex = targetEmpireIndex;
			}
			else if (this.TargetEmpireIndexVarName != string.Empty || this.TargetEmpireVarName != string.Empty)
			{
				this.Objective.TargetEmpireIndex = questBehaviour.Initiator.Index;
			}
		}
		if (this.SiegeTurnsNumberVarName != string.Empty)
		{
			int num2 = -1;
			if ((questBehaviour.TryGetQuestVariableValueByName<int>(this.SiegeTurnsNumberVarName, out num2) || int.TryParse(this.SiegeTurnsNumberVarName, out num2)) && num2 >= 0)
			{
				this.Objective.SiegeTurnsNumber = num2;
			}
		}
		if (this.TurnBeforeLeavingRegionVarName != string.Empty)
		{
			int num3 = -1;
			if ((questBehaviour.TryGetQuestVariableValueByName<int>(this.TurnBeforeLeavingRegionVarName, out num3) || int.TryParse(this.TurnBeforeLeavingRegionVarName, out num3)) && num3 >= 0)
			{
				this.Objective.TurnBeforeLeavingRegion = num3;
			}
		}
	}

	private void PrintVariables(QuestBehaviour questBehaviour)
	{
		Diagnostics.LogWarning("Quest BT variables: {0}", new object[]
		{
			questBehaviour.ToStringVariables()
		});
		Diagnostics.Log("[UpdateArmyObjective] Variables needed by UpdateArmyObjective:");
		if (this.TargetCityGUIDVarName != string.Empty)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] TargetCityGUIDVarName = {0}, GUID found = {1}", new object[]
			{
				this.TargetCityGUIDVarName,
				this.Objective.TargetCityGUID
			});
		}
		if (this.TargetCityVarName != string.Empty)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] TargetCityVarName = {0}, City found = {1}", new object[]
			{
				this.TargetCityVarName,
				this.Objective.TargetCityGUID
			});
		}
		if (this.TargetEmpireIndexVarName != string.Empty)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] TargetEmpireIndexVarName = {0}, Empire index found = {1}", new object[]
			{
				this.TargetEmpireIndexVarName,
				this.Objective.TargetEmpireIndex
			});
		}
		if (this.TargetEmpireVarName != string.Empty)
		{
			Diagnostics.LogWarning("[UpdateArmyObjective] TargetEmpireVarName = {0}, Empire index found = {1}", new object[]
			{
				this.TargetEmpireVarName,
				this.Objective.TargetEmpireIndex
			});
		}
	}

	[XmlIgnore]
	protected IGameEntityRepositoryService gameEntityRepositoryService;

	protected global::Game game;
}
