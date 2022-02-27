using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_SpawnArmies : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_SpawnArmies()
	{
		this.SpawnLocations = null;
		this.EmpireArmyOwner = null;
		this.EmpireArmyOwnerIndex = -1;
		this.EmpireArmyOwnerVarName = string.Empty;
		this.UseBehaviorInitiatorEmpire = false;
	}

	[XmlAttribute("ArmyDroplist")]
	public string ArmyDroplist { get; set; }

	[XmlAttribute("ArmyTag")]
	public string ArmyTag { get; set; }

	[XmlAttribute("ArmyUnitTag")]
	public string ArmyUnitTag { get; set; }

	[XmlAttribute("EmpireArmyOwnerVarName")]
	public string EmpireArmyOwnerVarName { get; set; }

	[XmlAttribute("ForbiddenSpawnLocationVarName")]
	public string ForbiddenSpawnLocationVarName { get; set; }

	[XmlAttribute("SpawnLocationVarName")]
	public string SpawnLocationVarName { get; set; }

	[XmlElement]
	public int EmpireArmyOwnerIndex { get; set; }

	[XmlElement]
	public WorldPosition[] SpawnLocations { get; set; }

	[XmlIgnore]
	private global::Empire EmpireArmyOwner { get; set; }

	[XmlAttribute("UseBehaviorInitiatorEmpire")]
	public bool UseBehaviorInitiatorEmpire { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		IEnumerable<WorldPosition> source;
		if (this.SpawnLocationVarName != null && questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.SpawnLocationVarName, out source))
		{
			this.SpawnLocations = source.ToArray<WorldPosition>();
		}
		if (this.SpawnLocations != null)
		{
			this.SpawnArmies(questBehaviour);
		}
		else
		{
			Diagnostics.LogError("Position to spawn is invalid: {0}", new object[]
			{
				questBehaviour.ToStringVariables()
			});
		}
		return State.Success;
	}

	protected void SpawnArmies(QuestBehaviour questBehaviour)
	{
		if (!string.IsNullOrEmpty(this.ArmyDroplist))
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				Diagnostics.LogError("Failed to retrieve the game service.");
				return;
			}
			global::Game game = service.Game as global::Game;
			if (game == null)
			{
				Diagnostics.LogError("Failed to cast gameService.Game to Game.");
				return;
			}
			IDatabase<Droplist> database = Databases.GetDatabase<Droplist>(false);
			if (database == null)
			{
				return;
			}
			string armyDroplist = this.ArmyDroplist;
			Droplist droplist;
			if (!database.TryGetValue(armyDroplist, out droplist))
			{
				Diagnostics.LogError("Cannot retrieve drop list '{0}' in quest definition '{1}'", new object[]
				{
					armyDroplist,
					questBehaviour.Quest.QuestDefinition.Name
				});
				return;
			}
			global::Empire empire = this.EmpireArmyOwner;
			if (empire == null || empire is LesserEmpire || this.UseBehaviorInitiatorEmpire)
			{
				empire = questBehaviour.Initiator;
			}
			if (this.UseBehaviorInitiatorEmpire)
			{
				this.EmpireArmyOwner = questBehaviour.Initiator;
			}
			Droplist droplist2;
			DroppableArmyDefinition droppableArmyDefinition = droplist.Pick(empire, out droplist2, new object[0]) as DroppableArmyDefinition;
			if (droppableArmyDefinition != null)
			{
				int num = 0;
				IDatabase<AnimationCurve> database2 = Databases.GetDatabase<AnimationCurve>(false);
				AnimationCurve animationCurve;
				if (database2 != null && database2.TryGetValue(QuestBehaviourTreeNode_Action_SpawnArmies.questUnitLevelEvolution, out animationCurve))
				{
					float propertyValue = questBehaviour.Initiator.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
					float num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, propertyValue, 1f);
					num = (int)num2;
					num = Math.Max(0, Math.Min(100, num));
				}
				StaticString[] unitDesignsNames = Array.ConvertAll<string, StaticString>(droppableArmyDefinition.UnitDesigns, (string input) => input);
				StaticString[] armyTags = new StaticString[]
				{
					this.ArmyTag
				};
				StaticString[] unitsTags = new StaticString[]
				{
					this.ArmyUnitTag
				};
				OrderSpawnArmies orderSpawnArmies = new OrderSpawnArmies(this.EmpireArmyOwner.Index, this.SpawnLocations.ToList<WorldPosition>().ToArray(), unitDesignsNames, 1, armyTags, unitsTags, num, QuestArmyObjective.QuestBehaviourType.Roaming);
				Diagnostics.Log("Posting order: {0}.", new object[]
				{
					orderSpawnArmies.ToString()
				});
				this.EmpireArmyOwner.PlayerControllers.Server.PostOrder(orderSpawnArmies);
			}
		}
	}

	protected void AddPositionToForbiddenSpawnPosition(QuestBehaviour questBehaviour, WorldPosition position)
	{
		if (!string.IsNullOrEmpty(this.ForbiddenSpawnLocationVarName))
		{
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.ForbiddenSpawnLocationVarName);
			List<WorldPosition> list = questVariable.Object as List<WorldPosition>;
			if (!list.Contains(position))
			{
				list.Add(position);
			}
		}
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
		IEnumerable<WorldPosition> source;
		if (this.SpawnLocations == null && questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.SpawnLocationVarName, out source))
		{
			this.SpawnLocations = source.ToArray<WorldPosition>();
		}
		if (!string.IsNullOrEmpty(this.ForbiddenSpawnLocationVarName) && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.ForbiddenSpawnLocationVarName))
		{
			QuestVariable questVariable = new QuestVariable(this.ForbiddenSpawnLocationVarName);
			questVariable.Object = new List<WorldPosition>();
			questBehaviour.QuestVariables.Add(questVariable);
		}
		if (string.IsNullOrEmpty(this.ArmyTag))
		{
			Diagnostics.LogError("Spawned armies requires an identifing tag");
			return false;
		}
		if (string.IsNullOrEmpty(this.ArmyUnitTag))
		{
			Diagnostics.LogError("Spawned armies requires an identifing tag");
			return false;
		}
		if (this.EmpireArmyOwnerIndex == -1)
		{
			if (!string.IsNullOrEmpty(this.EmpireArmyOwnerVarName))
			{
				global::Empire empireArmyOwner;
				if (!questBehaviour.TryGetQuestVariableValueByName<global::Empire>(this.EmpireArmyOwnerVarName, out empireArmyOwner))
				{
					Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
					{
						this.EmpireArmyOwnerVarName
					});
					return false;
				}
				this.EmpireArmyOwner = empireArmyOwner;
			}
			if (this.EmpireArmyOwner == null)
			{
				this.EmpireArmyOwner = game.Empires.FirstOrDefault((global::Empire match) => match.Name == "LesserEmpire#0");
				if (this.EmpireArmyOwner == null)
				{
					Diagnostics.LogError("Failed to retrieve the (lesser) quest empire.");
					return false;
				}
			}
			this.EmpireArmyOwnerIndex = this.EmpireArmyOwner.Index;
		}
		else
		{
			this.EmpireArmyOwner = game.Empires[this.EmpireArmyOwnerIndex];
		}
		return base.Initialize(questBehaviour);
	}

	[XmlIgnore]
	protected IGameEntityRepositoryService gameEntityRepositoryService;

	private static StaticString questUnitLevelEvolution = new StaticString("QuestUnitLevelEvolution");
}
