using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class QuestBehaviourTreeNode_Decorator_KillArmy : QuestBehaviourTreeNode_Decorator<EventEncounterStateChange>
{
	public QuestBehaviourTreeNode_Decorator_KillArmy()
	{
		this.EnemyArmyGUIDVarName = null;
		this.WinnerArmyGUID = GameEntityGUID.Zero;
		this.TargetOption = QuestBehaviourTreeNode_Decorator_KillArmy.KillArmyTargetOption.Any;
		this.CountAllDeadEnemyArmiesForStepProgression = false;
		this.FocusedEmpireVarName = string.Empty;
		this.SelectLooserWithTag = string.Empty;
	}

	[XmlAttribute("TargetOption")]
	public QuestBehaviourTreeNode_Decorator_KillArmy.KillArmyTargetOption TargetOption { get; set; }

	[XmlAttribute("CountAllDeadEnemyArmiesForStepProgression")]
	public bool CountAllDeadEnemyArmiesForStepProgression { get; set; }

	[XmlElement("EnemyArmyGUIDVarName")]
	public string[] EnemyArmyGUIDVarName { get; set; }

	[XmlAttribute]
	public string Output_LooserVarName { get; set; }

	[XmlAttribute("Output_WinnerVarName")]
	public string Output_WinnerVarName { get; set; }

	[XmlAttribute("FocusedEmpireVarName")]
	public string FocusedEmpireVarName { get; set; }

	[XmlElement]
	public ulong WinnerArmyGUID { get; set; }

	[XmlElement]
	public ulong LooserArmyGUID { get; set; }

	[XmlElement]
	public ulong[] EnemyArmyGUIDs { get; set; }

	[XmlElement]
	public int FocusedEmpireIndex { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, EventEncounterStateChange e, params object[] parameters)
	{
		if (this.UpdateVars)
		{
			this.Initialize(questBehaviour);
			this.UpdateVars = false;
		}
		if (e.EventArgs.EncounterState != EncounterState.BattleHasEnded)
		{
			return State.Running;
		}
		IEnumerable<Contender> enemiesContenderFromEmpire = e.EventArgs.Encounter.GetEnemiesContenderFromEmpire(e.Empire as global::Empire);
		if (enemiesContenderFromEmpire == null)
		{
			return State.Running;
		}
		this.SaveEnemyArmyGUIDs(questBehaviour);
		if (this.EnemyArmyGUIDVarName != null && this.EnemyArmyGUIDs == null)
		{
			Diagnostics.LogError("Fail to find all army GUID in '{0}' (for quest {1})", new object[]
			{
				string.Join(", ", this.EnemyArmyGUIDVarName),
				questBehaviour.Quest.Name
			});
			return State.Running;
		}
		if (this.EnemyArmyGUIDs == null || this.EnemyArmyGUIDs.Length == 0)
		{
			if (this.CountAllDeadEnemyArmiesForStepProgression)
			{
				base.ProgressionIncrement = enemiesContenderFromEmpire.Count((Contender match) => match.ContenderState == ContenderState.Defeated);
			}
			else
			{
				base.ProgressionIncrement = 1;
			}
		}
		else if (this.CountAllDeadEnemyArmiesForStepProgression)
		{
			base.ProgressionIncrement = this.ComputeDeadEnemyTargetCount(questBehaviour, e);
		}
		else
		{
			base.ProgressionIncrement = 1;
		}
		if (!string.IsNullOrEmpty(this.Output_WinnerVarName))
		{
			this.AddWinnerToQuestVariable(questBehaviour, e);
		}
		if (!string.IsNullOrEmpty(this.Output_LooserVarName))
		{
			this.AddLooserToQuestVariable(questBehaviour, e);
		}
		if (!string.IsNullOrEmpty(this.Output_WinnerEmpire))
		{
			this.AddWinnerEmpireToQuestVariable(questBehaviour, e);
		}
		if (!string.IsNullOrEmpty(this.Output_Position))
		{
			this.AddPositionToQuestVariable(questBehaviour, e);
		}
		if (!this.DoesKilledArmyBelongsToTargetedEmpire(questBehaviour, e))
		{
			return State.Running;
		}
		if ((this.TargetOption == QuestBehaviourTreeNode_Decorator_KillArmy.KillArmyTargetOption.All && this.AreAllTargetEnemyDead(questBehaviour, e)) || (this.TargetOption == QuestBehaviourTreeNode_Decorator_KillArmy.KillArmyTargetOption.Any && this.IsThereOneTargetEnemyDead(questBehaviour, e)))
		{
			return State.Success;
		}
		return State.Running;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (this.WinnerArmyGUID != GameEntityGUID.Zero && !questBehaviour.QuestVariables.Exists((QuestVariable match) => match.Name == this.Output_WinnerVarName))
		{
			QuestVariable questVariable = new QuestVariable(this.Output_WinnerVarName);
			questVariable.Object = this.WinnerArmyGUID;
			questBehaviour.QuestVariables.Add(questVariable);
		}
		this.SaveEnemyArmyGUIDs(questBehaviour);
		MajorEmpire majorEmpire;
		if (this.FocusedEmpireVarName != string.Empty && questBehaviour.TryGetQuestVariableValueByName<MajorEmpire>(this.FocusedEmpireVarName, out majorEmpire))
		{
			this.FocusedEmpireIndex = majorEmpire.Index;
		}
		if (this.UpdateVarName)
		{
			this.UpdateVars = true;
		}
		else
		{
			this.UpdateVars = false;
		}
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		return base.Initialize(questBehaviour);
	}

	private bool AreAllTargetEnemyDead(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		if (this.EnemyArmyGUIDs != null)
		{
			if (questBehaviour.Quest.QuestDefinition.Category != "GlobalQuest" && !this.IgnoreDisbandedArmies)
			{
				for (int i = 0; i < this.EnemyArmyGUIDs.Length; i++)
				{
					IGameEntity gameEntity;
					this.gameEntityRepositoryService.TryGetValue(this.EnemyArmyGUIDs[i], out gameEntity);
					if (gameEntity != null)
					{
						break;
					}
					if (gameEntity == null && i == this.EnemyArmyGUIDs.Length - 1)
					{
						return true;
					}
				}
			}
			bool flag = true;
			for (int j = 0; j < this.EnemyArmyGUIDs.Length; j++)
			{
				ulong enemyArmyGUID = this.EnemyArmyGUIDs[j];
				if (enemyArmyGUID == 0UL)
				{
					Diagnostics.LogError("Enemy contender corresponding to quest variable (varname: '{0}') isn't valid in quest definition (name: '{1}')", new object[]
					{
						this.EnemyArmyGUIDs[j],
						questBehaviour.Quest.QuestDefinition.Name
					});
				}
				else
				{
					Func<ContenderSnapShot, bool> <>9__2;
					Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault(delegate(Contender match)
					{
						IEnumerable<ContenderSnapShot> contenderSnapShots = match.ContenderSnapShots;
						Func<ContenderSnapShot, bool> predicate;
						if ((predicate = <>9__2) == null)
						{
							predicate = (<>9__2 = ((ContenderSnapShot snapshot) => snapshot.ContenderGUID == enemyArmyGUID));
						}
						return contenderSnapShots.Any(predicate);
					});
					if (contender == null)
					{
						return false;
					}
					if (contender.Empire != e.Empire)
					{
						if (contender.ContenderState != ContenderState.Defeated)
						{
							return false;
						}
						flag = false;
					}
				}
			}
			return !flag;
		}
		IEnumerable<Contender> enemiesContenderFromEmpire = e.EventArgs.Encounter.GetEnemiesContenderFromEmpire(e.Empire as global::Empire);
		if (enemiesContenderFromEmpire == null)
		{
			return false;
		}
		return enemiesContenderFromEmpire.All((Contender match) => match.ContenderState == ContenderState.Defeated);
	}

	private bool IsThereOneTargetEnemyDead(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		if (this.EnemyArmyGUIDs != null)
		{
			if (questBehaviour.Quest.QuestDefinition.Category != "GlobalQuest" && !this.IgnoreDisbandedArmies)
			{
				for (int i = 0; i < this.EnemyArmyGUIDs.Length; i++)
				{
					IGameEntity gameEntity;
					this.gameEntityRepositoryService.TryGetValue(this.EnemyArmyGUIDs[i], out gameEntity);
					if (gameEntity != null)
					{
						break;
					}
					if (gameEntity == null && i == this.EnemyArmyGUIDs.Length - 1)
					{
						return true;
					}
				}
			}
			for (int j = 0; j < this.EnemyArmyGUIDs.Length; j++)
			{
				ulong enemyArmyGUID = this.EnemyArmyGUIDs[j];
				if (enemyArmyGUID == 0UL)
				{
					Diagnostics.LogError("Enemy contender corresponding to quest variable (varname: '{0}') isn't valid in quest definition (name: '{1}')", new object[]
					{
						this.EnemyArmyGUIDs,
						questBehaviour.Quest.QuestDefinition.Name
					});
				}
				else
				{
					Func<ContenderSnapShot, bool> <>9__2;
					Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault(delegate(Contender match)
					{
						IEnumerable<ContenderSnapShot> contenderSnapShots = match.ContenderSnapShots;
						Func<ContenderSnapShot, bool> predicate;
						if ((predicate = <>9__2) == null)
						{
							predicate = (<>9__2 = ((ContenderSnapShot snapshot) => snapshot.ContenderGUID == enemyArmyGUID));
						}
						return contenderSnapShots.Any(predicate);
					});
					if (contender != null && contender.Empire != e.Empire && contender.ContenderState == ContenderState.Defeated)
					{
						return true;
					}
				}
			}
			return false;
		}
		IEnumerable<Contender> enemiesContenderFromEmpire = e.EventArgs.Encounter.GetEnemiesContenderFromEmpire(e.Empire as global::Empire);
		if (enemiesContenderFromEmpire == null)
		{
			return false;
		}
		return enemiesContenderFromEmpire.Any((Contender match) => match.ContenderState == ContenderState.Defeated);
	}

	private int ComputeDeadEnemyTargetCount(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		if (this.EnemyArmyGUIDs == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < this.EnemyArmyGUIDs.Length; i++)
		{
			ulong enemyArmyGUID = this.EnemyArmyGUIDs[i];
			if (enemyArmyGUID == 0UL)
			{
				Diagnostics.LogError("Enemy contender corresponding to quest variable (varname: '{0}') isn't valid in quest definition (name: '{1}')", new object[]
				{
					this.EnemyArmyGUIDs,
					questBehaviour.Quest.QuestDefinition.Name
				});
			}
			else
			{
				Func<ContenderSnapShot, bool> <>9__1;
				Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault(delegate(Contender match)
				{
					IEnumerable<ContenderSnapShot> contenderSnapShots = match.ContenderSnapShots;
					Func<ContenderSnapShot, bool> predicate;
					if ((predicate = <>9__1) == null)
					{
						predicate = (<>9__1 = ((ContenderSnapShot snapshot) => snapshot.ContenderGUID == enemyArmyGUID));
					}
					return contenderSnapShots.Any(predicate);
				});
				if (contender != null && contender.Empire != e.Empire && contender.ContenderState == ContenderState.Defeated)
				{
					num++;
				}
			}
		}
		return num;
	}

	private void AddWinnerToQuestVariable(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault((Contender match) => match.Empire == questBehaviour.Initiator && match.ContenderState == ContenderState.Survived);
		if (contender != null)
		{
			this.WinnerArmyGUID = contender.GUID;
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_WinnerVarName);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.Output_WinnerVarName);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			DepartmentOfDefense agency = questBehaviour.Initiator.GetAgency<DepartmentOfDefense>();
			Diagnostics.Assert(agency != null);
			Army army = agency.Armies.FirstOrDefault((Army match) => match.GUID == this.WinnerArmyGUID);
			if (army == null)
			{
				Diagnostics.LogError("Decorator_KillArmy: the army (GUID:'{0}') cannot be found in the empire (index:'{1}') armies", new object[]
				{
					this.WinnerArmyGUID,
					questBehaviour.Initiator.Index
				});
			}
			questVariable.Object = army;
		}
	}

	private void AddLooserToQuestVariable(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		if (this.SelectLooserWithTag != string.Empty)
		{
			using (IEnumerator<Contender> enumerator = (from match in e.EventArgs.Encounter.Contenders
			where match.ContenderState == ContenderState.Defeated
			select match).GetEnumerator())
			{
				while (enumerator.MoveNext())
				{
					Contender contender = enumerator.Current;
					if (contender.Garrison.HasTag(this.SelectLooserWithTag))
					{
						this.LooserArmyGUID = contender.GUID;
						QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_LooserVarName);
						if (questVariable == null)
						{
							questVariable = new QuestVariable(this.Output_LooserVarName);
							questBehaviour.QuestVariables.Add(questVariable);
						}
						questVariable.Object = contender.Garrison;
					}
				}
				return;
			}
		}
		Contender contender2 = e.EventArgs.Encounter.Contenders.FirstOrDefault((Contender match) => match.ContenderState == ContenderState.Defeated);
		if (contender2 != null)
		{
			this.LooserArmyGUID = contender2.GUID;
			QuestVariable questVariable2 = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_LooserVarName);
			if (questVariable2 == null)
			{
				questVariable2 = new QuestVariable(this.Output_LooserVarName);
				questBehaviour.QuestVariables.Add(questVariable2);
			}
			questVariable2.Object = contender2.Garrison;
		}
	}

	private void SaveEnemyArmyGUIDs(QuestBehaviour questBehaviour)
	{
		if (this.EnemyArmyGUIDVarName != null && this.EnemyArmyGUIDs == null)
		{
			this.EnemyArmyGUIDs = new ulong[this.EnemyArmyGUIDVarName.Length];
			for (int i = 0; i < this.EnemyArmyGUIDVarName.Length; i++)
			{
				ulong num = GameEntityGUID.Zero;
				if (!questBehaviour.TryGetQuestVariableValueByName<ulong>(this.EnemyArmyGUIDVarName[i], out num))
				{
					this.EnemyArmyGUIDs = null;
					return;
				}
				this.EnemyArmyGUIDs[i] = num;
			}
		}
	}

	private bool DoesKilledArmyBelongsToTargetedEmpire(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault((Contender match) => match.ContenderState == ContenderState.Defeated);
		return !(this.FocusedEmpireVarName != string.Empty) || contender == null || contender.Empire.Index == this.FocusedEmpireIndex;
	}

	private void AddWinnerEmpireToQuestVariable(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault((Contender match) => match.ContenderState == ContenderState.Survived);
		if (contender != null)
		{
			this.WinnerEmpireIndex = contender.Empire.Index;
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_WinnerEmpire);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.Output_WinnerEmpire);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = this.WinnerEmpireIndex;
		}
	}

	private void AddPositionToQuestVariable(QuestBehaviour questBehaviour, EventEncounterStateChange e)
	{
		Contender contender = e.EventArgs.Encounter.Contenders.FirstOrDefault((Contender match) => match.ContenderState == ContenderState.Defeated);
		if (contender != null)
		{
			this.BattlePosition = contender.WorldPosition;
			QuestVariable questVariable = questBehaviour.QuestVariables.FirstOrDefault((QuestVariable match) => match.Name == this.Output_Position);
			if (questVariable == null)
			{
				questVariable = new QuestVariable(this.Output_Position);
				questBehaviour.QuestVariables.Add(questVariable);
			}
			questVariable.Object = this.BattlePosition;
		}
	}

	[XmlAttribute("Output_WinnerEmpire")]
	public string Output_WinnerEmpire { get; set; }

	[XmlAttribute("Output_Position")]
	public string Output_Position { get; set; }

	[XmlAttribute("UpdateVarName")]
	public bool UpdateVarName { get; set; }

	[XmlElement]
	public int WinnerEmpireIndex { get; set; }

	[XmlElement]
	public WorldPosition BattlePosition { get; set; }

	[XmlElement]
	public bool UpdateVars { get; set; }

	[XmlAttribute]
	public string SelectLooserWithTag { get; set; }

	public override void Release()
	{
		base.Release();
		this.gameEntityRepositoryService = null;
	}

	[XmlAttribute]
	public bool IgnoreDisbandedArmies { get; set; }

	private IGameEntityRepositoryService gameEntityRepositoryService;

	[Flags]
	public enum KillArmyTargetOption
	{
		Any = 0,
		All = 1
	}
}
