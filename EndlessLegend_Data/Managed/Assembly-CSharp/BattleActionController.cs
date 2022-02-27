using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Simulation;

public class BattleActionController : IDisposable, IInstructionContainer
{
	public BattleActionController(BattleSimulation battleSimulation)
	{
		this.Simulation = battleSimulation;
	}

	~BattleActionController()
	{
		this.Dispose(false);
	}

	public BattleSimulation Simulation { get; private set; }

	public List<IUnitReportInstruction> ReportInstructions
	{
		get
		{
			return this.reportInstructions;
		}
	}

	public void Dispose()
	{
		this.Dispose(true);
	}

	public bool ExecuteBattleAction(BattleAction battleAction, BattleEffects[] battleEffectsList, BattleSimulationUnit[] initiators, BattleSimulationTarget[] currentTargets, bool immediate)
	{
		if (battleAction == null)
		{
			throw new ArgumentNullException("battleAction");
		}
		if (battleEffectsList == null)
		{
			throw new ArgumentNullException("battleEffectsList");
		}
		if (initiators == null)
		{
			throw new ArgumentNullException("initiators");
		}
		bool flag = false;
		foreach (BattleSimulationUnit initiator in initiators)
		{
			flag |= this.ExecuteBattleAction(battleAction, battleEffectsList, initiator, currentTargets, immediate, null);
		}
		return flag;
	}

	public bool ExecuteBattleAction(BattleAction battleAction, BattleEffects[] battleEffectsList, BattleSimulationUnit referenceUnit, WorldPosition targetPosition, bool immediate)
	{
		if (battleAction == null)
		{
			throw new ArgumentNullException("battleAction");
		}
		if (battleEffectsList == null)
		{
			throw new ArgumentNullException("battleEffectsList");
		}
		if (referenceUnit == null)
		{
			throw new ArgumentNullException("initiators");
		}
		BattleSimulationTarget[] currentTargets = new BattleSimulationTarget[]
		{
			new BattleSimulationTarget(targetPosition)
		};
		return this.ExecuteBattleAction(battleAction, battleEffectsList, referenceUnit, currentTargets, immediate, null);
	}

	public bool ExecuteBattleAction(BattleAction battleAction, BattleEffects[] battleEffectsList, BattleSimulationUnit initiator, BattleSimulationTarget[] currentTargets, bool immediate, List<IUnitReportInstruction> executionInstructions = null)
	{
		return this.ExecuteBattleAction(battleAction, battleEffectsList, initiator, initiator, currentTargets, immediate, executionInstructions);
	}

	public bool ExecuteBattleAction(BattleAction battleAction, BattleEffects[] battleEffectsList, BattleSimulationUnit initiator, BattleSimulationUnit activator, BattleSimulationTarget[] currentTargets, bool immediate, List<IUnitReportInstruction> executionInstructions = null)
	{
		if (battleAction == null)
		{
			throw new ArgumentNullException("battleAction");
		}
		if (battleEffectsList == null)
		{
			throw new ArgumentNullException("battleEffectsList");
		}
		this.reportCopy = executionInstructions;
		float battleActionContextRandomNumber = (float)this.Simulation.BattleSimulationRandom.NextDouble();
		bool flag = false;
		foreach (BattleEffects battleEffects in battleEffectsList)
		{
			flag |= this.ExecuteBattleAction(battleActionContextRandomNumber, battleAction, battleEffects, initiator, activator, currentTargets, immediate);
		}
		this.reportCopy = null;
		return flag;
	}

	public void ExecuteBattleEffect(BattleSimulationUnit initiator, BattleSimulationUnit activator, BattleSimulationTarget target, BattleEffect battleEffect, BattleActionContext actionContext)
	{
		Diagnostics.Assert(this.battleEffectsContexts != null, "this.battleEffectsContexts != null");
		BattleEffectController battleEffectController;
		if (this.battleEffectsContexts.ContainsKey(target))
		{
			battleEffectController = this.battleEffectsContexts[target];
		}
		else
		{
			battleEffectController = new BattleEffectController(target, this.Simulation);
			this.battleEffectsContexts.Add(target, battleEffectController);
		}
		Diagnostics.Assert(battleEffectController != null);
		battleEffectController.Execute(battleEffect, initiator, activator, this, actionContext);
	}

	public BattleAction.State GetBattleActionState(BattleAction battleAction)
	{
		if (this.pendingAction.Any((BattleActionContext context) => context.BattleAction == battleAction))
		{
			return BattleAction.State.Selected;
		}
		Diagnostics.Assert(this.activeBattleActions != null);
		BattleActionContext battleActionContext = this.activeBattleActions.Find((BattleActionContext context) => context.BattleAction == battleAction);
		if (battleActionContext != null)
		{
			return battleActionContext.State;
		}
		return BattleAction.State.Available;
	}

	public ActivePeriod[] GetTargetActivePeriods(BattleSimulationTarget target)
	{
		List<ActivePeriod> list = new List<ActivePeriod>();
		Diagnostics.Assert(this.activeBattleActions != null);
		for (int i = 0; i < this.activeBattleActions.Count; i++)
		{
			BattleActionContext battleActionContext = this.activeBattleActions[i];
			Diagnostics.Assert(battleActionContext != null && battleActionContext.Targets != null);
			if (battleActionContext.Targets.Contains(target))
			{
				Diagnostics.Assert(battleActionContext.ActivePeriods != null);
				list.AddRange(battleActionContext.ActivePeriods);
			}
		}
		return list.ToArray();
	}

	public void OnRoundEnd()
	{
		this.reportInstructions.Clear();
		Diagnostics.Assert(this.battleEffectsContexts != null);
		foreach (BattleEffectController battleEffectController in this.battleEffectsContexts.Values)
		{
			Diagnostics.Assert(battleEffectController != null);
			battleEffectController.OnRoundEnd(this);
		}
		for (int i = this.activeBattleActions.Count - 1; i >= 0; i--)
		{
			Diagnostics.Assert(this.activeBattleActions[i] != null);
			this.activeBattleActions[i].OnRoundEnd(this);
			if (this.activeBattleActions[i].State == BattleAction.State.Finished)
			{
				this.activeBattleActions.RemoveAt(i);
			}
		}
	}

	public void OnRoundStart()
	{
		Diagnostics.Assert(this.activeBattleActions != null);
		Diagnostics.Assert(this.battleEffectsContexts != null);
		foreach (BattleEffectController battleEffectController in this.battleEffectsContexts.Values)
		{
			Diagnostics.Assert(battleEffectController != null);
			battleEffectController.OnRoundStart(this);
		}
		for (int i = 0; i < this.activeBattleActions.Count; i++)
		{
			Diagnostics.Assert(this.activeBattleActions[i] != null);
			this.activeBattleActions[i].OnRoundStart(this);
		}
		Diagnostics.Assert(this.pendingAction != null);
		while (this.pendingAction.Count > 0)
		{
			BattleActionContext battleActionContext = this.pendingAction.Dequeue();
			this.ExecuteBattleAction(battleActionContext, true);
		}
	}

	public void OnUnitDeath(BattleSimulationUnit unit, List<IUnitReportInstruction> executionInstructions = null)
	{
		this.reportCopy = executionInstructions;
		foreach (BattleEffectController battleEffectController in this.battleEffectsContexts.Values)
		{
			Diagnostics.Assert(battleEffectController != null);
			battleEffectController.OnUnitDeath(unit, this);
		}
		this.reportCopy = null;
	}

	public void ReleaseBattleActions()
	{
		for (int i = 0; i < this.activeBattleActions.Count; i++)
		{
			Diagnostics.Assert(this.activeBattleActions[i] != null);
			this.activeBattleActions[i].ReleaseBattleAction(this);
		}
		Diagnostics.Assert(this.battleEffectsContexts != null);
		foreach (BattleEffectController battleEffectController in this.battleEffectsContexts.Values)
		{
			Diagnostics.Assert(battleEffectController != null);
			battleEffectController.ReleaseBattleEffect(this);
		}
	}

	public void ReportInstruction(IUnitReportInstruction reportInstruction)
	{
		if (reportInstruction == null)
		{
			throw new ArgumentNullException("reportInstruction");
		}
		Diagnostics.Assert(this.reportInstructions != null);
		BattleActionUpdateInstruction battleActionUpdateInstruction = reportInstruction as BattleActionUpdateInstruction;
		if (battleActionUpdateInstruction != null)
		{
			if (this.reportCopy != null)
			{
				this.reportCopy.RemoveAll((IUnitReportInstruction instruction) => instruction.UnitGUID == reportInstruction.UnitGUID && instruction is BattleActionUpdateInstruction && (instruction as BattleActionUpdateInstruction).BattleActionName == battleActionUpdateInstruction.BattleActionName);
			}
			this.reportInstructions.RemoveAll((IUnitReportInstruction instruction) => instruction.UnitGUID == reportInstruction.UnitGUID && instruction is BattleActionUpdateInstruction && (instruction as BattleActionUpdateInstruction).BattleActionName == battleActionUpdateInstruction.BattleActionName);
		}
		BattleEffectUpdateInstruction battleEffectUpdateInstruction = reportInstruction as BattleEffectUpdateInstruction;
		if (battleEffectUpdateInstruction != null && !battleEffectUpdateInstruction.IsCumulable)
		{
			if (this.reportCopy != null)
			{
				this.reportCopy.RemoveAll((IUnitReportInstruction instruction) => instruction.UnitGUID == reportInstruction.UnitGUID && instruction is BattleEffectUpdateInstruction && (instruction as BattleEffectUpdateInstruction).BattleEffectName == battleEffectUpdateInstruction.BattleEffectName && (instruction as BattleEffectUpdateInstruction).TargetGUID == battleEffectUpdateInstruction.TargetGUID && (instruction as BattleEffectUpdateInstruction).TargetWorldPosition == battleEffectUpdateInstruction.TargetWorldPosition);
			}
			this.reportInstructions.RemoveAll((IUnitReportInstruction instruction) => instruction.UnitGUID == reportInstruction.UnitGUID && instruction is BattleEffectUpdateInstruction && (instruction as BattleEffectUpdateInstruction).BattleEffectName == battleEffectUpdateInstruction.BattleEffectName && (instruction as BattleEffectUpdateInstruction).TargetGUID == battleEffectUpdateInstruction.TargetGUID && (instruction as BattleEffectUpdateInstruction).TargetWorldPosition == battleEffectUpdateInstruction.TargetWorldPosition);
		}
		if (this.reportCopy != null)
		{
			this.reportCopy.Add(reportInstruction);
		}
		this.reportInstructions.Add(reportInstruction);
	}

	public void DoReportInstructions(RoundReport currentReport, bool postRound = false)
	{
		if (currentReport == null)
		{
			throw new ArgumentNullException("currentReport");
		}
		Diagnostics.Assert(this.reportInstructions != null);
		Diagnostics.Assert(this.Simulation != null);
		for (int i = 0; i < this.reportInstructions.Count; i++)
		{
			IUnitReportInstruction unitReportInstruction = this.reportInstructions[i];
			Diagnostics.Assert(unitReportInstruction != null);
			BattleSimulationUnit battleUnit = this.Simulation.GetBattleUnit(unitReportInstruction.UnitGUID);
			currentReport.DoReportInstruction(battleUnit, unitReportInstruction, postRound);
		}
		this.reportInstructions.Clear();
	}

	public void SetReportCopy(List<IUnitReportInstruction> instructionList)
	{
		this.reportCopy = instructionList;
	}

	protected virtual void Dispose(bool disposing)
	{
		if (disposing)
		{
		}
		this.activeBattleActions.Clear();
		this.battleEffectsContexts.Clear();
		this.pendingAction.Clear();
		if (this.reportCopy != null)
		{
			this.reportCopy.Clear();
			this.reportCopy = null;
		}
		this.reportInstructions.Clear();
		this.Simulation = null;
	}

	private void ExecuteAOEBattleAction(float battleActionContextRandomNumber, BattleAction battleAction, BattleEffectsArea battleEffectsArea, BattleSimulationUnit initiator, BattleSimulationUnit activator, BattleSimulationTarget[] aoeCenterTargets, bool immediate)
	{
		if (battleAction == null)
		{
			throw new ArgumentNullException("battleAction");
		}
		if (battleEffectsArea == null)
		{
			throw new ArgumentNullException("battleEffectsArea");
		}
		if (aoeCenterTargets == null || aoeCenterTargets.Length == 0)
		{
			return;
		}
		List<BattleSimulationTarget> list = new List<BattleSimulationTarget>();
		foreach (BattleSimulationTarget battleSimulationTarget3 in aoeCenterTargets)
		{
			WorldOrientation orientation = (battleSimulationTarget3.Unit == null) ? WorldOrientation.Undefined : battleSimulationTarget3.Unit.Orientation;
			List<WorldPosition> list2 = null;
			if (battleEffectsArea.Type == BattleEffectsArea.AreaType.Chain)
			{
				Diagnostics.Assert(this.Simulation != null);
				BattleSimulationTarget[] array = null;
				if (battleSimulationTarget3.Unit != null)
				{
					array = this.Simulation.FilterTargets(BattleEffects.TargetFlags.SameGroup, battleSimulationTarget3.Unit, null);
				}
				if (array != null && array.Length > 0)
				{
					list2 = new List<WorldPosition>(array.Length);
					list2.AddRange(from battleSimulationTarget in array
					select battleSimulationTarget.DynamicPosition);
				}
			}
			SimulationObject context = null;
			if (initiator != null)
			{
				context = initiator.SimulationObject;
			}
			Diagnostics.Assert(this.Simulation != null);
			IPathfindingArea area = battleEffectsArea.GetArea(battleSimulationTarget3.DynamicPosition, orientation, list2, this.Simulation.WorldParameters, this.Simulation.BattleZone, context);
			if (area == null)
			{
				Diagnostics.LogError("Unknown area of effect {0}, the effect will be applied on the filtered unit.", new object[]
				{
					battleEffectsArea.Type
				});
			}
			else
			{
				WorldPosition[] worldPositions = area.GetWorldPositions(this.Simulation.WorldParameters);
				if (worldPositions == null)
				{
					Diagnostics.LogError("Can't get area of effect positions.");
				}
				else
				{
					list.Clear();
					for (int j = 0; j < worldPositions.Length; j++)
					{
						BattleSimulationUnit unitFromPosition = this.Simulation.GetUnitFromPosition(worldPositions[j], false);
						if (unitFromPosition != null)
						{
							BattleSimulationTarget battleSimulationTarget2 = new BattleSimulationTarget(unitFromPosition);
							if (battleEffectsArea.AvoidCastingUnit)
							{
								bool flag = false;
								for (int k = 0; k < aoeCenterTargets.Length; k++)
								{
									if (aoeCenterTargets[k].DynamicPosition == battleSimulationTarget2.DynamicPosition)
									{
										if (battleEffectsArea.Type != BattleEffectsArea.AreaType.Chain)
										{
											flag = true;
											break;
										}
										battleSimulationTarget2.Ignore = true;
									}
								}
								if (flag)
								{
									goto IL_23E;
								}
							}
							list.Add(battleSimulationTarget2);
						}
						IL_23E:;
					}
					BattleActionAOEInstruction battleActionAOEInstruction = new BattleActionAOEInstruction(initiator.UnitGUID, battleEffectsArea.RealizationVisualEffectName);
					battleActionAOEInstruction.RealizationApplicationMethod = battleEffectsArea.RealizationApplicationMethod;
					battleActionAOEInstruction.RealizationApplicationData = battleEffectsArea.RealizationApplicationData;
					this.ReportInstruction(battleActionAOEInstruction);
					List<IUnitReportInstruction> list3 = this.reportCopy;
					this.reportCopy = battleActionAOEInstruction.ReportInstructions;
					Diagnostics.Assert(battleEffectsArea.BattleEffects != null);
					for (int l = 0; l < battleEffectsArea.BattleEffects.Length; l++)
					{
						BattleEffects battleEffects = battleEffectsArea.BattleEffects[l];
						this.ExecuteBattleAction(battleActionContextRandomNumber, battleAction, battleEffects, initiator, activator, list.ToArray(), immediate);
					}
					this.reportCopy = list3;
				}
			}
		}
	}

	private bool ExecuteBattleAction(float battleActionContextRandomNumber, BattleAction battleAction, BattleEffects battleEffects, BattleSimulationUnit initiator, BattleSimulationUnit activator, BattleSimulationTarget[] currentTargets, bool immediate)
	{
		if (battleAction == null)
		{
			throw new ArgumentNullException("battleAction");
		}
		if (battleEffects == null)
		{
			throw new ArgumentNullException("battleEffects");
		}
		if (!battleAction.CanBeAppliedByDeadUnit && activator.GetPropertyValue(SimulationProperties.Health) <= 0f)
		{
			return false;
		}
		Diagnostics.Assert(this.Simulation != null && this.Simulation.BattleSimulationRandom != null);
		if (this.Simulation.BattleSimulationRandom.NextDouble() >= (double)battleEffects.GetProbability(initiator))
		{
			return false;
		}
		BattleSimulationTarget[] array = this.Simulation.FilterTargets(battleEffects.TargetsFilter, initiator, currentTargets);
		BattleEffectsArea battleEffectsArea = battleEffects as BattleEffectsArea;
		if (battleEffectsArea == null)
		{
			BattleActionContext battleActionContext = new BattleActionContext(this, battleAction, battleEffects, initiator, activator, array, battleActionContextRandomNumber);
			this.ExecuteBattleAction(battleActionContext, immediate);
		}
		else
		{
			this.ExecuteAOEBattleAction(battleActionContextRandomNumber, battleAction, battleEffectsArea, initiator, activator, array, immediate);
		}
		return true;
	}

	private void ExecuteBattleAction(BattleActionContext battleActionContext, bool immediate)
	{
		if (battleActionContext == null)
		{
			throw new ArgumentNullException("battleActionContext");
		}
		if (immediate)
		{
			Diagnostics.Assert(this.activeBattleActions != null);
			this.activeBattleActions.Add(battleActionContext);
			battleActionContext.OnRoundStart(this);
		}
		else
		{
			Diagnostics.Assert(this.pendingAction != null);
			this.pendingAction.Enqueue(battleActionContext);
		}
	}

	private readonly List<BattleActionContext> activeBattleActions = new List<BattleActionContext>();

	private readonly Dictionary<BattleSimulationTarget, BattleEffectController> battleEffectsContexts = new Dictionary<BattleSimulationTarget, BattleEffectController>();

	private readonly Queue<BattleActionContext> pendingAction = new Queue<BattleActionContext>();

	private readonly List<IUnitReportInstruction> reportInstructions = new List<IUnitReportInstruction>();

	private List<IUnitReportInstruction> reportCopy;
}
