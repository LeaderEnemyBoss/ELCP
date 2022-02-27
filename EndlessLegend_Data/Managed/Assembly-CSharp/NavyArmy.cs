using System;
using System.Collections.Generic;
using UnityEngine;

public class NavyArmy : BaseNavyArmy
{
	public NavyArmy(AILayer_Navy navyLayer)
	{
		this.navyLayer = navyLayer;
		this.Opportunities = new List<BehaviorOpportunity>();
	}

	public WorldPath PathToSecondaryTarget { get; set; }

	public BehaviorOpportunity SecondaryTarget { get; set; }

	public List<BehaviorOpportunity> Opportunities { get; set; }

	public WorldPosition SafePosition { get; internal set; }

	public WorldPath PathToSafePosition { get; set; }

	public override void DisplayDebug()
	{
		base.DisplayDebug();
		if (this.OpportunityAttackableTarget != null)
		{
			GUILayout.Label(string.Format("Opportunity Attackable Target: {0}", this.OpportunityAttackableTarget.LocalizedName), new GUILayoutOption[0]);
		}
		if (this.SecondaryTarget != null)
		{
			GUILayout.Label(string.Format("Opportunity Target: {0}", this.SecondaryTarget.OpportunityPosition), new GUILayoutOption[0]);
		}
		for (int i = 0; i < this.Opportunities.Count; i++)
		{
			BehaviorOpportunity behaviorOpportunity = this.Opportunities[i];
			behaviorOpportunity.Score.Display("Opportunity[{0}] = {1}: {2} at {3}", new object[]
			{
				i,
				behaviorOpportunity.Score.Value,
				behaviorOpportunity.Type,
				behaviorOpportunity.OpportunityPosition
			});
		}
		GUILayout.Space(10f);
	}

	public AILayer_Navy NavyLayer
	{
		get
		{
			return this.navyLayer;
		}
	}

	public override bool IsActive
	{
		get
		{
			return (this.army != null && this.army.CurrentAutoAction == Army.AutoActionsEnum.AutoExplore) || base.IsActive;
		}
		set
		{
			base.IsActive = value;
		}
	}

	public override void UpdateState()
	{
		base.UpdateState();
		this.SecondaryTarget = null;
	}

	public override void AssignCommander(BaseNavyCommander commander)
	{
		NavyCommander navyCommander = base.Commander as NavyCommander;
		if (navyCommander != null)
		{
			navyCommander.NavyArmies.Remove(this);
		}
		base.AssignCommander(commander);
		navyCommander = (base.Commander as NavyCommander);
		if (navyCommander != null)
		{
			navyCommander.NavyArmies.Add(this);
		}
	}

	public override void Initialize()
	{
		base.Initialize();
		this.army = (base.Garrison as Army);
		if (this.army != null)
		{
			this.army.AutoActionChange += this.Army_AutoActionChange;
		}
	}

	public override void Release()
	{
		base.Release();
		if (this.army != null)
		{
			this.army.AutoActionChange -= this.Army_AutoActionChange;
			this.army = null;
		}
	}

	protected override ArmyBehavior GetDefaultBehavior()
	{
		return new NavyBehavior_Roaming();
	}

	protected override void FilterTasks()
	{
		base.TaskEvaluations.Clear();
		if (base.Garrison.CurrentUnitSlot > 0 && base.Commander != null)
		{
			for (int i = 0; i < this.navyLayer.NavyTasks.Count; i++)
			{
				if (this.navyLayer.NavyTasks[i].CheckValidity())
				{
					NavyTaskEvaluation navyTaskEvaluation = this.navyLayer.NavyTasks[i].ComputeFitness(this);
					if (base.CurrentMainTask == this.navyLayer.NavyTasks[i] || this.navyLayer.NavyTasks[i].AssignedArmy == this)
					{
						navyTaskEvaluation.Fitness.Boost(0.2f, "Already assigned", new object[0]);
					}
					base.TaskEvaluations.Add(navyTaskEvaluation);
				}
			}
		}
	}

	private void Army_AutoActionChange(object sender, EventArgs e)
	{
		NavyTask_AutoAction navyTask_AutoAction = this.navyLayer.FindTask<NavyTask_AutoAction>((NavyTask_AutoAction match) => match.NavyArmy == this);
		if (this.army != null && this.army.CurrentAutoAction == Army.AutoActionsEnum.AutoExplore)
		{
			base.BehaviorState = ArmyWithTask.ArmyBehaviorState.NeedRun;
			this.State = TickableState.NeedTick;
			if (navyTask_AutoAction == null)
			{
				navyTask_AutoAction = new NavyTask_AutoAction(this.navyLayer, this);
				this.navyLayer.NavyTasks.Add(navyTask_AutoAction);
			}
			this.Assign(navyTask_AutoAction, new HeuristicValue(1f));
		}
		else if (navyTask_AutoAction != null)
		{
			if (navyTask_AutoAction.AssignedArmy != null)
			{
				navyTask_AutoAction.AssignedArmy.Unassign();
			}
			this.navyLayer.NavyTasks.Remove(navyTask_AutoAction);
		}
	}

	public IGarrisonWithPosition OpportunityAttackableTarget { get; set; }

	private AILayer_Navy navyLayer;

	private Army army;
}
