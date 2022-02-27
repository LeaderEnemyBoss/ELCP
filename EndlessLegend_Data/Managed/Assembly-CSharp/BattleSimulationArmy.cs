using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Simulation;

public class BattleSimulationArmy : SimulationObjectWrapper
{
	public BattleSimulationArmy(SimulationObject simulationObject, BattleContender contender) : base(simulationObject)
	{
		if (simulationObject == null)
		{
			throw new ArgumentNullException("simulationObject");
		}
		if (contender == null)
		{
			throw new ArgumentNullException("contender");
		}
		this.contender = contender;
		if (contender.DefaultTargetingStrategy != null)
		{
			StaticString armyBattleTargetingStrategy = contender.DefaultTargetingStrategy;
			this.ArmyBattleTargetingStrategy = armyBattleTargetingStrategy;
			this.ArmyBattleTargetingWantedStrategy = armyBattleTargetingStrategy;
		}
		else
		{
			StaticString armyBattleTargetingStrategy = BattleEncounter.GetDefaultStrategy();
			this.ArmyBattleTargetingWantedStrategy = armyBattleTargetingStrategy;
			this.ArmyBattleTargetingStrategy = armyBattleTargetingStrategy;
		}
		this.WaitingArmyActions = new List<BattleArmyAction>();
	}

	public StaticString ArmyBattleTargetingStrategy { get; set; }

	public StaticString ArmyBattleTargetingWantedStrategy
	{
		get
		{
			return this.armyBattleTargetingWantedStrategy;
		}
		set
		{
			this.armyBattleTargetingWantedStrategy = value;
			BattleTargetingStrategyChangeInstruction reportInstruction = new BattleTargetingStrategyChangeInstruction(this.ContenderGUID, this.armyBattleTargetingWantedStrategy);
			this.ReportInstruction(reportInstruction);
		}
	}

	public int BestUnitRoundRank { get; set; }

	public BattleContender Contender
	{
		get
		{
			Diagnostics.Assert(this.contender != null);
			return this.contender;
		}
	}

	public GameEntityGUID ContenderGUID
	{
		get
		{
			Diagnostics.Assert(this.contender != null);
			return this.contender.GUID;
		}
	}

	public byte Group
	{
		get
		{
			Diagnostics.Assert(this.contender != null);
			return this.contender.Group;
		}
	}

	public bool IsDead
	{
		get
		{
			if (this.units == null)
			{
				return true;
			}
			for (int i = 0; i < this.units.Count; i++)
			{
				if (!this.units[i].IsDead)
				{
					return false;
				}
			}
			return true;
		}
	}

	public int AliveUnitsCount
	{
		get
		{
			if (this.units == null)
			{
				return 0;
			}
			int num = 0;
			for (int i = 0; i < this.units.Count; i++)
			{
				if (!this.units[i].IsDead)
				{
					num++;
				}
			}
			return num;
		}
	}

	public bool HasUnitsThatCanFight
	{
		get
		{
			if (this.units == null)
			{
				return false;
			}
			for (int i = 0; i < this.units.Count; i++)
			{
				if (this.units[i].Position.IsValid)
				{
					if (!this.units[i].IsDead && !this.units[i].SimulationObject.Tags.Contains("UnitActionPhoenixToEgg"))
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool HasFightingUnitAlive
	{
		get
		{
			if (this.units == null)
			{
				return false;
			}
			for (int i = 0; i < this.units.Count; i++)
			{
				if (this.units[i].Position.IsValid)
				{
					if (!this.units[i].IsDead)
					{
						return true;
					}
				}
			}
			return false;
		}
	}

	public bool HasWaitingReinforcements
	{
		get
		{
			if (this.units == null)
			{
				return false;
			}
			for (int i = 0; i < this.units.Count; i++)
			{
				if (!this.units[i].Position.IsValid)
				{
					return true;
				}
			}
			return false;
		}
	}

	public List<BattleArmyAction> WaitingArmyActions { get; private set; }

	public ReadOnlyCollection<WorldPosition> ReinforcementPoints
	{
		get
		{
			Diagnostics.Assert(this.reinforcementPoints != null);
			return this.reinforcementPoints.AsReadOnly();
		}
	}

	public ReadOnlyCollection<BattleSimulationUnit> Units
	{
		get
		{
			Diagnostics.Assert(this.units != null);
			return this.units.AsReadOnly();
		}
	}

	public UnitBodyDefinition PriorityUnitBodyForReinforcement { get; private set; }

	public void AddReinforcementPoint(WorldPosition reinforcementPointPosition)
	{
		if (!reinforcementPointPosition.IsValid)
		{
			throw new ArgumentException("reinforcementPointPosition should be a valid position", "reinforcementPointPosition");
		}
		Diagnostics.Assert(this.reinforcementPoints != null);
		this.reinforcementPoints.Add(reinforcementPointPosition);
	}

	public void AddUnit(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.units != null);
		if (!this.units.Contains(unit))
		{
			unit.UpdateBattleTargetingUnitBehaviorWeight();
			this.units.Add(unit);
			this.RefreshAttachment(unit);
		}
	}

	public void AddWaitingSpell(StaticString spellName, StaticString actionName, BattleAction battleAction, WorldPosition targetPosition)
	{
		this.WaitingArmyActions.Add(new BattleArmyAction_Spell(spellName, actionName, battleAction, targetPosition));
	}

	public void AddWaitingAction(StaticString actionName, List<BattleAction> battleActions, WorldPosition targetPosition)
	{
		this.WaitingArmyActions.Add(new BattleArmyAction(actionName, battleActions, targetPosition));
	}

	public void AddWaitingAction(StaticString actionName, BattleAction battleAction, WorldPosition targetPosition)
	{
		this.WaitingArmyActions.Add(new BattleArmyAction(actionName, battleAction, targetPosition));
	}

	public void ClearWaitingActions()
	{
		this.WaitingArmyActions.Clear();
	}

	public void ExecuteWaitingActions(BattleSimulation battleSimulation, RoundReport roundReport)
	{
		BattleSimulationUnit battleSimulationUnit = this.Units.FirstOrDefault((BattleSimulationUnit match) => match.Position.IsValid);
		if (battleSimulationUnit == null)
		{
			return;
		}
		List<IUnitReportInstruction> list = new List<IUnitReportInstruction>();
		for (int i = 0; i < this.WaitingArmyActions.Count; i++)
		{
			int num = BattleSimulation.UnitStateNextID++;
			BattleArmyAction battleArmyAction = this.WaitingArmyActions[i];
			ContenderActionInstruction contenderActionInstruction = battleArmyAction.BuildContenderActionInstruction(this.ContenderGUID, num, battleSimulationUnit.UnitGUID);
			WorldOrientation orientation = this.Contender.WorldOrientation;
			if (contenderActionInstruction is ContenderActionSpellInstruction)
			{
				orientation = this.Contender.Deployment.DeploymentArea.Forward;
			}
			if (contenderActionInstruction != null)
			{
				contenderActionInstruction.AddEffectPosition(battleArmyAction.TargetPosition);
				BattleSimulationTarget[] currentTargets = new BattleSimulationTarget[]
				{
					new BattleSimulationTarget(battleArmyAction.TargetPosition)
				};
				for (int j = 0; j < battleArmyAction.BattleActions.Count; j++)
				{
					BattleAction battleAction = battleArmyAction.BattleActions[j];
					for (int k = 0; k < battleAction.BattleEffects.Length; k++)
					{
						BattleEffects battleEffects = battleAction.BattleEffects[k];
						if (battleEffects is BattleEffectsArea)
						{
							IPathfindingArea area = (battleEffects as BattleEffectsArea).GetArea(battleArmyAction.TargetPosition, orientation, null, battleSimulation.WorldParameters, battleSimulation.BattleZone, battleSimulationUnit);
							if (area != null)
							{
								WorldPosition[] worldPositions = area.GetWorldPositions(battleSimulation.WorldParameters);
								if (worldPositions != null)
								{
									contenderActionInstruction.AddEffectArea(worldPositions);
								}
							}
						}
					}
					WorldOrientation orientation2 = battleSimulationUnit.Orientation;
					battleSimulationUnit.Orientation = orientation;
					battleSimulation.BattleActionController.ExecuteBattleAction(battleAction, battleAction.BattleEffects, battleSimulationUnit, currentTargets, true, list);
					battleSimulationUnit.Orientation = orientation2;
					battleSimulation.CheckUnitsDeath(num);
				}
				for (int l = 0; l < list.Count; l++)
				{
					contenderActionInstruction.AddReportInstruction(list[l]);
				}
				roundReport.ReportInstruction(this.ContenderGUID, contenderActionInstruction);
			}
		}
		this.ClearWaitingActions();
	}

	public void OnRoundBegin()
	{
		if (this.ArmyBattleTargetingStrategy != this.ArmyBattleTargetingWantedStrategy)
		{
			this.ArmyBattleTargetingStrategy = this.ArmyBattleTargetingWantedStrategy;
		}
	}

	public void RefreshAttachment(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(base.SimulationObject != null);
		bool flag = base.SimulationObject.Children != null && base.SimulationObject.Children.Contains(unit.SimulationObject);
		bool isDead = unit.IsDead;
		if (unit.Position.IsValid)
		{
			if (isDead && flag)
			{
				base.SimulationObject.RemoveChild(unit.SimulationObject);
				this.Refresh(false);
			}
			else if (!isDead && !flag)
			{
				base.SimulationObject.AddChild(unit.SimulationObject);
				this.Refresh(false);
			}
		}
		else if (flag)
		{
			base.SimulationObject.RemoveChild(unit.SimulationObject);
			this.Refresh(false);
		}
	}

	public void RemoveUnit(BattleSimulationUnit unit)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		Diagnostics.Assert(this.units != null);
		if (this.units.Contains(unit))
		{
			this.units.Remove(unit);
		}
	}

	public void ReportInstruction(IContenderReportInstruction reportInstruction)
	{
		if (reportInstruction == null)
		{
			throw new ArgumentNullException("reportInstruction");
		}
		Diagnostics.Assert(this.reportInstructions != null);
		BattleTargetingStrategyChangeInstruction battleTargetingStrategyChangeInstruction = reportInstruction as BattleTargetingStrategyChangeInstruction;
		if (battleTargetingStrategyChangeInstruction != null)
		{
			this.reportInstructions.RemoveAll((IContenderReportInstruction instruction) => instruction is BattleTargetingStrategyChangeInstruction);
		}
		BattleReinforcementPriorityChangeInstruction battleReinforcementPriorityChangeInstruction = reportInstruction as BattleReinforcementPriorityChangeInstruction;
		if (battleReinforcementPriorityChangeInstruction != null)
		{
			this.reportInstructions.RemoveAll((IContenderReportInstruction instruction) => instruction is BattleReinforcementPriorityChangeInstruction);
		}
		this.reportInstructions.Add(reportInstruction);
	}

	public void ReportInstructions(RoundReport currentReport)
	{
		if (currentReport == null)
		{
			throw new ArgumentNullException("currentReport");
		}
		Diagnostics.Assert(this.reportInstructions != null);
		for (int i = 0; i < this.reportInstructions.Count; i++)
		{
			IContenderReportInstruction contenderReportInstruction = this.reportInstructions[i];
			Diagnostics.Assert(contenderReportInstruction != null);
			currentReport.ReportInstruction(this.ContenderGUID, contenderReportInstruction);
		}
		this.reportInstructions.Clear();
	}

	public bool SetUnitBodyAsPioritary(UnitBodyDefinition unitBodyDefinition)
	{
		this.PriorityUnitBodyForReinforcement = unitBodyDefinition;
		this.ReportInstruction(new BattleReinforcementPriorityChangeInstruction(this.ContenderGUID)
		{
			PriorityUnitBodyDefinitionReference = ((unitBodyDefinition == null) ? StaticString.Empty : unitBodyDefinition.Name)
		});
		return true;
	}

	public void SpawnUnit(BattleSimulationUnit unit, WorldPosition spawnPosition)
	{
		if (unit == null)
		{
			throw new ArgumentNullException("unit");
		}
		if (!spawnPosition.IsValid)
		{
			throw new ArgumentException("The spawnPosition must be a valid position.", "spawnPosition");
		}
		Diagnostics.Assert(this.units != null);
		Diagnostics.Assert(this.units.Contains(unit));
		unit.Spawn(spawnPosition);
		this.RefreshAttachment(unit);
	}

	protected StaticString armyBattleTargetingWantedStrategy;

	private readonly BattleContender contender;

	private readonly List<WorldPosition> reinforcementPoints = new List<WorldPosition>();

	private readonly List<IContenderReportInstruction> reportInstructions = new List<IContenderReportInstruction>();

	private readonly List<BattleSimulationUnit> units = new List<BattleSimulationUnit>();
}
