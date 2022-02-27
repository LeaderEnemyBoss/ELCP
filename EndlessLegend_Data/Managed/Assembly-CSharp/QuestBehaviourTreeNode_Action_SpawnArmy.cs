using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Action_SpawnArmy : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_SpawnArmy()
	{
		this.SpawnLocations = null;
		this.ArmyGUID = GameEntityGUID.Zero;
		this.EmpireArmyOwner = null;
		this.EmpireArmyOwnerIndex = -1;
		this.EmpireArmyOwnerVarName = string.Empty;
		this.ArmyDroplistSuffixVarName = string.Empty;
		this.ArmyDroplistSuffix = string.Empty;
		this.CanMoveOnSpawn = true;
		this.HaveActionPointOnSpawn = true;
		this.DroplistRewardOnDeathName = string.Empty;
		this.UseBehaviorInitiatorEmpire = false;
	}

	[XmlAttribute("ArmyDroplist")]
	public string ArmyDroplist { get; set; }

	[XmlAttribute("ArmyDroplistSuffixVarName")]
	public string ArmyDroplistSuffixVarName { get; set; }

	[XmlAttribute("HaveActionPointOnSpawn")]
	public bool HaveActionPointOnSpawn { get; set; }

	[XmlAttribute("CanMoveOnSpawn")]
	public bool CanMoveOnSpawn { get; set; }

	[XmlAttribute("DroplistRewardOnDeathName")]
	public string DroplistRewardOnDeathName { get; set; }

	[XmlAttribute("EmpireArmyOwnerVarName")]
	public string EmpireArmyOwnerVarName { get; set; }

	[XmlAttribute]
	public bool ForceGlobalArmySymbol { get; set; }

	[XmlAttribute("ForbiddenSpawnLocationVarName")]
	public string ForbiddenSpawnLocationVarName { get; set; }

	[XmlAttribute("Output_EnemyArmyGUIDVarName")]
	public string OutputEnemyArmyGUIDVarName { get; set; }

	[XmlAttribute]
	public bool OutputEnemyArmyGUIDIsGlobal { get; set; }

	[XmlAttribute("SpawnLocationVarName")]
	public string SpawnLocationVarName { get; set; }

	[XmlElement]
	public string ArmyDroplistSuffix { get; set; }

	[XmlElement]
	public ulong ArmyGUID { get; set; }

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
		if (this.SpawnLocationVarName != null)
		{
			IEnumerable<WorldPosition> source;
			WorldPosition worldPosition;
			if (questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.SpawnLocationVarName, out source))
			{
				this.SpawnLocations = source.ToArray<WorldPosition>();
			}
			else if (questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.SpawnLocationVarName, out worldPosition))
			{
				this.SpawnLocations = new WorldPosition[]
				{
					worldPosition
				};
			}
		}
		if (this.SpawnLocations != null)
		{
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService != null && this.gameEntityRepositoryService.TryGetValue(this.ArmyGUID, out gameEntity) && gameEntity != null)
			{
				return State.Success;
			}
			this.SpawnArmy(questBehaviour);
			if (!string.IsNullOrEmpty(this.DroplistRewardOnDeathName))
			{
				IGameService service = Services.GetService<IGameService>();
				Diagnostics.Assert(service != null && service.Game != null && service.Game is global::Game);
				IQuestRewardRepositoryService service2 = (service.Game as global::Game).GetService<IQuestRewardRepositoryService>();
				Diagnostics.Assert(service2 != null);
				service2.AddRewardForArmyKill(this.ArmyGUID, this.DroplistRewardOnDeathName);
			}
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

	protected void SpawnArmy(QuestBehaviour questBehaviour)
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
			string text = this.ArmyDroplist;
			if (this.ArmyDroplistSuffix != string.Empty)
			{
				text = text + "_" + this.ArmyDroplistSuffix;
			}
			Droplist droplist;
			if (!database.TryGetValue(text, out droplist))
			{
				Diagnostics.LogError("Cannot retrieve drop list '{0}' in quest definition '{1}'", new object[]
				{
					text,
					questBehaviour.Quest.QuestDefinition.Name
				});
				return;
			}
			if (!string.IsNullOrEmpty(this.EmpireArmyOwnerVarName))
			{
				global::Empire empire = null;
				object obj;
				if (questBehaviour.TryGetQuestVariableValueByName<object>(this.EmpireArmyOwnerVarName, out obj))
				{
					if (obj is global::Empire)
					{
						empire = (obj as global::Empire);
					}
					else if (obj is int)
					{
						empire = game.Empires[(int)obj];
					}
					if (empire != null)
					{
						this.EmpireArmyOwner = empire;
					}
				}
			}
			global::Empire empire2 = this.EmpireArmyOwner;
			if (empire2 == null || empire2 is LesserEmpire || this.UseBehaviorInitiatorEmpire)
			{
				empire2 = questBehaviour.Initiator;
			}
			if (this.UseBehaviorInitiatorEmpire)
			{
				this.EmpireArmyOwner = questBehaviour.Initiator;
			}
			Droplist droplist2;
			DroppableArmyDefinition droppableArmyDefinition = droplist.Pick(empire2, out droplist2, new object[0]) as DroppableArmyDefinition;
			if (droppableArmyDefinition != null)
			{
				int num = 0;
				IDatabase<AnimationCurve> database2 = Databases.GetDatabase<AnimationCurve>(false);
				AnimationCurve animationCurve;
				if (database2 != null && database2.TryGetValue(QuestBehaviourTreeNode_Action_SpawnArmy.questUnitLevelEvolution, out animationCurve))
				{
					float propertyValue = questBehaviour.Initiator.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
					float num2 = animationCurve.EvaluateWithScaledAxis((float)game.Turn, propertyValue, 1f);
					num = (int)num2;
					num = Math.Max(0, Math.Min(100, num));
				}
				StaticString[] array = Array.ConvertAll<string, StaticString>(droppableArmyDefinition.UnitDesigns, (string input) => input);
				bool flag = false;
				IDatabase<UnitDesign> database3 = Databases.GetDatabase<UnitDesign>(false);
				for (int i = 0; i < array.Length; i++)
				{
					UnitDesign unitDesign;
					if (database3.TryGetValue(array[i], out unitDesign) && unitDesign != null && unitDesign.Tags.Contains(DownloadableContent16.TagSeafaring))
					{
						flag = true;
						break;
					}
				}
				IEnumerable<WorldPosition> enumerable = null;
				questBehaviour.TryGetQuestVariableValueByName<WorldPosition>(this.ForbiddenSpawnLocationVarName, out enumerable);
				List<WorldPosition> list = this.SpawnLocations.ToList<WorldPosition>().Randomize(null);
				IWorldPositionningService service2 = game.Services.GetService<IWorldPositionningService>();
				IPathfindingService service3 = game.Services.GetService<IPathfindingService>();
				Diagnostics.Assert(service2 != null);
				WorldPosition worldPosition = WorldPosition.Invalid;
				if (!questBehaviour.Quest.QuestDefinition.IsGlobal)
				{
					PathfindingMovementCapacity pathfindingMovementCapacity = PathfindingMovementCapacity.Water;
					if (!flag)
					{
						pathfindingMovementCapacity |= PathfindingMovementCapacity.Ground;
					}
					for (int j = 0; j < list.Count; j++)
					{
						WorldPosition worldPosition2 = list[j];
						if (DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition2, pathfindingMovementCapacity))
						{
							if (enumerable != null)
							{
								if (enumerable.Contains(worldPosition2))
								{
									goto IL_355;
								}
								this.AddPositionToForbiddenSpawnPosition(questBehaviour, worldPosition2);
							}
							worldPosition = worldPosition2;
							break;
						}
						IL_355:;
					}
					if (!service3.IsTileStopable(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water, PathfindingFlags.IgnoreFogOfWar))
					{
						worldPosition = WorldPosition.Invalid;
					}
					if (!worldPosition.IsValid && list.Count > 0)
					{
						List<WorldPosition> list2 = new List<WorldPosition>();
						Queue<WorldPosition> queue = new Queue<WorldPosition>();
						worldPosition = list[0];
						bool flag2 = false;
						if (worldPosition.IsValid)
						{
							flag2 = service2.IsWaterTile(worldPosition);
						}
						do
						{
							if (queue.Count > 0)
							{
								worldPosition = queue.Dequeue();
							}
							for (int k = 0; k < 6; k++)
							{
								WorldPosition neighbourTileFullCyclic = service2.GetNeighbourTileFullCyclic(worldPosition, (WorldOrientation)k, 1);
								if (!list2.Contains(neighbourTileFullCyclic) && list2.Count < 19)
								{
									queue.Enqueue(neighbourTileFullCyclic);
									list2.Add(neighbourTileFullCyclic);
								}
							}
							if (!DepartmentOfDefense.CheckWhetherTargetPositionIsValidForUseAsArmySpawnLocation(worldPosition, pathfindingMovementCapacity) || !service3.IsTileStopable(worldPosition, PathfindingMovementCapacity.Ground | PathfindingMovementCapacity.Water, PathfindingFlags.IgnoreFogOfWar) || flag2 != service2.IsWaterTile(worldPosition))
							{
								worldPosition = WorldPosition.Invalid;
							}
						}
						while (worldPosition == WorldPosition.Invalid && queue.Count > 0);
					}
					if (!worldPosition.IsValid)
					{
						string format = "Cannot find a valid position to spawn on: {0}";
						object[] array2 = new object[1];
						array2[0] = string.Join(",", (from position in list
						select position.ToString()).ToArray<string>());
						Diagnostics.LogError(format, array2);
						return;
					}
				}
				OrderSpawnArmy orderSpawnArmy;
				if (worldPosition.IsValid)
				{
					orderSpawnArmy = new OrderSpawnArmy(this.EmpireArmyOwner.Index, worldPosition, num, true, this.CanMoveOnSpawn, true, questBehaviour.Quest.QuestDefinition.IsGlobal || this.ForceGlobalArmySymbol, array);
				}
				else
				{
					orderSpawnArmy = new OrderSpawnArmy(this.EmpireArmyOwner.Index, list.ToArray(), num, true, this.CanMoveOnSpawn, true, questBehaviour.Quest.QuestDefinition.IsGlobal || this.ForceGlobalArmySymbol, array);
				}
				orderSpawnArmy.GameEntityGUID = this.ArmyGUID;
				Diagnostics.Log("Posting order: {0}.", new object[]
				{
					orderSpawnArmy.ToString()
				});
				this.EmpireArmyOwner.PlayerControllers.Server.PostOrder(orderSpawnArmy);
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
		if (this.ArmyGUID != GameEntityGUID.Zero && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.OutputEnemyArmyGUIDVarName))
		{
			QuestVariable questVariable2 = new QuestVariable(this.OutputEnemyArmyGUIDVarName);
			questVariable2.Object = this.ArmyGUID;
			questBehaviour.QuestVariables.Add(questVariable2);
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
		if (this.ArmyDroplistSuffixVarName != string.Empty && this.ArmyDroplistSuffix == string.Empty)
		{
			string text;
			if (!questBehaviour.TryGetQuestVariableValueByName<string>(this.ArmyDroplistSuffixVarName, out text))
			{
				Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}', are you sure it is a string?)", new object[]
				{
					this.ArmyDroplistSuffixVarName
				});
				return false;
			}
			if (text == string.Empty)
			{
				Diagnostics.LogError("The suffix is an empty string (varname: '{0}')", new object[]
				{
					this.ArmyDroplistSuffixVarName
				});
				return false;
			}
			this.ArmyDroplistSuffix = text;
		}
		if (this.ArmyGUID == 0UL && !string.IsNullOrEmpty(this.OutputEnemyArmyGUIDVarName))
		{
			QuestVariable questVariable3 = questBehaviour.GetQuestVariableByName(this.OutputEnemyArmyGUIDVarName);
			if (questVariable3 == null)
			{
				questVariable3 = new QuestVariable(this.OutputEnemyArmyGUIDVarName);
				questBehaviour.QuestVariables.Add(questVariable3);
			}
			this.ArmyGUID = this.gameEntityRepositoryService.GenerateGUID();
			questVariable3.Object = this.ArmyGUID;
			if (this.OutputEnemyArmyGUIDIsGlobal)
			{
				IQuestManagementService service2 = game.Services.GetService<IQuestManagementService>();
				if (service2 != null)
				{
					QuestVariable questVariable4 = new QuestVariable(this.OutputEnemyArmyGUIDVarName, this.ArmyGUID);
					service2.State.AddGlobalVariable(questBehaviour.Initiator.Index, questVariable4);
				}
			}
		}
		return base.Initialize(questBehaviour);
	}

	[XmlIgnore]
	protected IGameEntityRepositoryService gameEntityRepositoryService;

	private static StaticString questUnitLevelEvolution = new StaticString("QuestUnitLevelEvolution");
}
