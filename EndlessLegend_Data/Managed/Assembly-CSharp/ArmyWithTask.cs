using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public abstract class ArmyWithTask : ITickable
{
	public IGarrisonWithPosition MainAttackableTarget { get; set; }

	public WorldPath PathToMainTarget { get; set; }

	public bool Foldout { get; set; }

	public virtual void DisplayDebug()
	{
		if (this.Garrison == null)
		{
			GUILayout.Label("Army has been destroyed.", new GUILayoutOption[0]);
		}
		else
		{
			GUILayout.Label(string.Format("Military power: {0}", this.Garrison.GetPropertyValue(SimulationProperties.MilitaryPower).ToString("0.0")), new GUILayoutOption[0]);
		}
		GUILayout.Label(string.Format("BehaviorState : {0} | TickState : {1}", this.BehaviorState.ToString(), this.State.ToString()), new GUILayoutOption[0]);
		if (this.CurrentMainTask == null)
		{
			GUILayout.Label(string.Format("Main task: none", new object[0]), new GUILayoutOption[0]);
		}
		else
		{
			this.CurrentMainTask.CurrentAssignationFitness.Display("Main task = {0}: {1}", new object[]
			{
				this.CurrentMainTask.CurrentAssignationFitness.Value,
				this.CurrentMainTask.GetDebugTitle()
			});
			if (this.MainAttackableTarget != null)
			{
				WorldPosition worldPosition = this.MainAttackableTarget.WorldPosition;
				GUILayout.Label(string.Format("Main Target: {0} (at {1})", this.MainAttackableTarget.LocalizedName, worldPosition), new GUILayoutOption[0]);
			}
			if (this.PathToMainTarget != null)
			{
				GUILayout.Label(string.Format("Current main path: {0}", this.PathToMainTarget.ToString()), new GUILayoutOption[0]);
			}
		}
		if (this.DebugNode != null)
		{
			this.DisplayBehavior(this.DebugNode);
		}
		GUILayout.Space(10f);
	}

	private void DisplayBehavior(BehaviorNodeDebug nodeDebug)
	{
		string text = string.Format("{0} ({1})", nodeDebug.NodeName, nodeDebug.NodeState);
		if (nodeDebug.Children != null)
		{
			nodeDebug.IsFoldout = GUILayout.Toggle(nodeDebug.IsFoldout, text, new GUILayoutOption[0]);
			if (nodeDebug.IsFoldout)
			{
				GUILayout.BeginHorizontal(new GUILayoutOption[0]);
				GUILayout.Space(15f);
				GUILayout.BeginVertical(new GUILayoutOption[0]);
				for (int i = 0; i < nodeDebug.Children.Length; i++)
				{
					this.DisplayBehavior(nodeDebug.Children[i]);
				}
				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
				return;
			}
		}
		else
		{
			GUILayout.Label(text, new GUILayoutOption[0]);
		}
	}

	public ArmyTask CurrentMainTask { get; set; }

	public virtual bool IsActive { get; set; }

	public ArmyWithTask.ArmyBehaviorState BehaviorState { get; set; }

	public TickableState State { get; set; }

	public IGarrisonWithPosition Garrison { get; set; }

	public BehaviorNodeDebug DebugNode { get; set; }

	public virtual void Initialize()
	{
		this.tickableRepositoryHelper = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.tickableRepositoryHelper.Register(this);
		this.State = TickableState.NeedTick;
		this.BehaviorState = ArmyWithTask.ArmyBehaviorState.NeedRun;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		this.encounterRepositoryService = service.Game.Services.GetService<IEncounterRepositoryService>();
		this.defaultBehavior = this.GetDefaultBehavior();
		if (this.defaultBehavior != null)
		{
			this.defaultBehavior.Initialize();
		}
	}

	public virtual void Release()
	{
		this.Unassign();
		if (this.tickableRepositoryHelper != null)
		{
			this.tickableRepositoryHelper.Unregister(this);
			this.tickableRepositoryHelper = null;
		}
		this.Garrison = null;
	}

	public virtual void Tick()
	{
		if (!this.IsActive || this.Garrison == null)
		{
			this.State = TickableState.NoTick;
			return;
		}
		if (this.Garrison.IsInEncounter)
		{
			return;
		}
		Army army = this.Garrison as Army;
		if (army != null)
		{
			if (army.IsMoving)
			{
				return;
			}
			if (army.IsLocked)
			{
				return;
			}
		}
		if (this.encounterRepositoryService != null)
		{
			IEnumerable<Encounter> enumerable = this.encounterRepositoryService;
			if (enumerable != null && enumerable.Any((Encounter encounter) => encounter.IsGarrisonInEncounter(this.Garrison.GUID, false)))
			{
				return;
			}
		}
		if (this.CurrentMainTask == null && this.ValidateMainTask())
		{
			return;
		}
		this.ExecuteMainTask();
		if (this.BehaviorState == ArmyWithTask.ArmyBehaviorState.Succeed)
		{
			this.Unassign();
			this.State = TickableState.NeedTick;
			return;
		}
		if (this.CurrentMainTask != null && !this.CurrentMainTask.CheckValidity())
		{
			this.Unassign();
			this.State = TickableState.NeedTick;
			return;
		}
		if (this.BehaviorState == ArmyWithTask.ArmyBehaviorState.NeedRun)
		{
			this.State = TickableState.NeedTick;
			return;
		}
		if (this.BehaviorState == ArmyWithTask.ArmyBehaviorState.Optional)
		{
			this.State = TickableState.Optional;
			return;
		}
		this.State = TickableState.NoTick;
	}

	public virtual void Unassign()
	{
		if (this.CurrentMainTask != null)
		{
			if (this.CurrentMainTask.AssignedArmy == this)
			{
				this.CurrentMainTask.AssignedArmy = null;
			}
			this.State = TickableState.NeedTick;
			this.BehaviorState = ArmyWithTask.ArmyBehaviorState.NeedRun;
			this.CurrentMainTask = null;
			if (this.defaultBehavior != null)
			{
				this.defaultBehavior.Reset();
			}
		}
	}

	public virtual void Assign(ArmyTask task, HeuristicValue fitness)
	{
		if (this.CurrentMainTask == task)
		{
			return;
		}
		if (this.CurrentMainTask != null)
		{
			this.Unassign();
		}
		if (task != null)
		{
			if (task.AssignedArmy != null)
			{
				task.AssignedArmy.Unassign();
			}
			this.CurrentMainTask = task;
			this.CurrentMainTask.AssignedArmy = this;
			this.CurrentMainTask.CurrentAssignationFitness = fitness;
			IGameService service = Services.GetService<IGameService>();
			this.CurrentMainTask.EstimatedTurnEnd = new HeuristicValue(0f);
			this.CurrentMainTask.EstimatedTurnEnd.Add((float)(service.Game as global::Game).Turn, "current turn", new object[0]);
			this.CurrentMainTask.EstimatedTurnEnd.Add(5f, "estimated time to complete (constant)", new object[0]);
			this.CurrentMainTask.Behavior.Reset();
			this.State = TickableState.NeedTick;
		}
	}

	public abstract bool ValidateMainTask();

	protected abstract ArmyBehavior GetDefaultBehavior();

	protected virtual void ExecuteMainTask()
	{
		if (this.CurrentMainTask != null)
		{
			if (this.CurrentMainTask.TargetGuid.IsValid && (this.MainAttackableTarget == null || this.MainAttackableTarget.GUID != this.CurrentMainTask.TargetGuid))
			{
				IGameEntity gameEntity = null;
				this.gameEntityRepositoryService.TryGetValue(this.CurrentMainTask.TargetGuid, out gameEntity);
				this.MainAttackableTarget = (gameEntity as IGarrisonWithPosition);
			}
			int num = (int)this.CurrentMainTask.Behavior.Behave(this);
			if (this.DebugNode == null || !this.DebugNode.IsFoldout)
			{
				this.DebugNode = this.CurrentMainTask.Behavior.DumpDebug();
			}
			if (num == 3)
			{
				this.State = TickableState.NeedTick;
				return;
			}
			this.CurrentMainTask.Behavior.Reset();
			return;
		}
		else
		{
			if (this.defaultBehavior == null)
			{
				this.DebugNode = null;
				this.State = TickableState.NoTick;
				this.BehaviorState = ArmyWithTask.ArmyBehaviorState.Sleep;
				return;
			}
			int num2 = (int)this.defaultBehavior.Behave(this);
			if (this.DebugNode == null || !this.DebugNode.IsFoldout)
			{
				this.DebugNode = this.defaultBehavior.DumpDebug();
			}
			if (num2 == 3)
			{
				this.State = TickableState.NeedTick;
				return;
			}
			this.defaultBehavior.Reset();
			return;
		}
	}

	private ArmyBehavior defaultBehavior;

	private ITickableRepositoryAIHelper tickableRepositoryHelper;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IEncounterRepositoryService encounterRepositoryService;

	public enum ArmyBehaviorState
	{
		NeedRun,
		Optional,
		Sleep,
		Succeed
	}
}
