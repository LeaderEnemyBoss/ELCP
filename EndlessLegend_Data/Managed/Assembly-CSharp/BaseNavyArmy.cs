using System;
using System.Collections.Generic;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public abstract class BaseNavyArmy : ArmyWithTask, IXmlSerializable
{
	public BaseNavyArmy()
	{
		this.UnitRatioLimit = new ArmyRatioLimit(0.75f, 0.25f, 1f);
	}

	public WorldPosition RoamingNextPosition { get; set; }

	public WorldPath PathToRoamingPosition { get; set; }

	public override void DisplayDebug()
	{
		GUILayout.Label(string.Format("Role : {0} | Size : {1}", this.Role, this.ArmySize), new GUILayoutOption[0]);
		if (this.Commander != null)
		{
			GUILayout.Label(string.Format("Region : {0}", this.Commander.RegionData.WaterRegion.LocalizedName), new GUILayoutOption[0]);
		}
		base.DisplayDebug();
		for (int i = 0; i < this.TaskEvaluations.Count; i++)
		{
			if (this.TaskEvaluations[i].Task != base.CurrentMainTask)
			{
				this.TaskEvaluations[i].Fitness.Display(string.Format("Task[{0}] = {1}: {2}", i, this.TaskEvaluations[i].Fitness.Value, this.TaskEvaluations[i].Task.GetDebugTitle()), new object[0]);
			}
		}
		GUILayout.Space(10f);
	}

	public virtual void ReadXml(XmlReader reader)
	{
		reader.ReadStartElement();
	}

	public virtual void WriteXml(XmlWriter writer)
	{
		writer.WriteVersionAttribute(3);
		if (base.Garrison != null)
		{
			writer.WriteAttributeString<GameEntityGUID>("GarrisonGuid", base.Garrison.GUID);
		}
		if (this.Commander != null)
		{
			writer.WriteAttributeString<int>("CommanderRegionIndex", this.Commander.RegionData.WaterRegionIndex);
		}
	}

	public List<NavyTaskEvaluation> TaskEvaluations
	{
		get
		{
			return this.taskEvaluations;
		}
	}

	public BaseNavyTask CurrentNavyTask
	{
		get
		{
			return base.CurrentMainTask as BaseNavyTask;
		}
	}

	public BaseNavyCommander Commander { get; private set; }

	public ArmyRatioLimit UnitRatioLimit { get; set; }

	public BaseNavyArmy.ArmyState ArmySize { get; set; }

	public BaseNavyArmy.ArmyRole Role { get; set; }

	public virtual float GetMaximumMovement()
	{
		return base.Garrison.GetPropertyValue(SimulationProperties.MaximumMovementOnWater);
	}

	public virtual void UpdateState()
	{
		if (!base.Garrison.HasOnlySeafaringUnits(false))
		{
			this.ArmySize = BaseNavyArmy.ArmyState.Low;
			return;
		}
		float num = (float)base.Garrison.StandardUnits.Count;
		num /= (float)base.Garrison.MaximumUnitSlot;
		if (num == 0f)
		{
			this.ArmySize = BaseNavyArmy.ArmyState.Empty;
		}
		else if (num >= this.UnitRatioLimit.UnitRatioForFull)
		{
			this.ArmySize = BaseNavyArmy.ArmyState.Full;
		}
		else if (num <= this.UnitRatioLimit.UnitRatioForLow)
		{
			this.ArmySize = BaseNavyArmy.ArmyState.Low;
		}
		else if (num >= this.UnitRatioLimit.UnitRatioForHigh)
		{
			this.ArmySize = BaseNavyArmy.ArmyState.High;
		}
		else
		{
			this.ArmySize = BaseNavyArmy.ArmyState.Medium;
		}
	}

	public virtual void UpdateRole()
	{
		if (!base.Garrison.HasSeafaringUnits())
		{
			this.Role = BaseNavyArmy.ArmyRole.Land;
			return;
		}
		if (!base.Garrison.HasOnlySeafaringUnits(false))
		{
			this.Role = BaseNavyArmy.ArmyRole.Convoi;
			return;
		}
		if (this.Role == BaseNavyArmy.ArmyRole.TaskForce)
		{
			if (this.ArmySize < BaseNavyArmy.ArmyState.Medium)
			{
				this.Role = BaseNavyArmy.ArmyRole.Renfort;
				return;
			}
		}
		else if (this.ArmySize <= BaseNavyArmy.ArmyState.Medium)
		{
			this.Role = BaseNavyArmy.ArmyRole.Renfort;
			return;
		}
		this.Role = BaseNavyArmy.ArmyRole.TaskForce;
	}

	public override bool ValidateMainTask()
	{
		this.FilterTasks();
		this.taskEvaluations.Sort((NavyTaskEvaluation left, NavyTaskEvaluation right) => -1 * left.Fitness.CompareTo(right.Fitness));
		NavyTaskEvaluation navyTaskEvaluation = this.taskEvaluations.Find((NavyTaskEvaluation match) => match.Task.AssignedArmy != null && match.Task.AssignedArmy == this);
		if (navyTaskEvaluation != null)
		{
		}
		if (navyTaskEvaluation == null)
		{
			navyTaskEvaluation = this.taskEvaluations.Find((NavyTaskEvaluation match) => match.Task.AssignedArmy == null && match.Fitness > 0f);
			for (int i = 0; i < this.taskEvaluations.Count; i++)
			{
				if (this.taskEvaluations[i].Fitness <= 0f)
				{
					break;
				}
				float num = this.taskEvaluations[i].Fitness;
				if (this.taskEvaluations[i].Task.AssignedArmy == null || num >= this.taskEvaluations[i].Task.CurrentAssignationFitness)
				{
					navyTaskEvaluation = this.taskEvaluations[i];
					break;
				}
			}
		}
		bool result = false;
		if (navyTaskEvaluation != null)
		{
			if (navyTaskEvaluation.Task.AssignedArmy != this || base.CurrentMainTask != navyTaskEvaluation.Task)
			{
				this.Assign(navyTaskEvaluation.Task, navyTaskEvaluation.Fitness);
				result = true;
			}
			navyTaskEvaluation.Task.CurrentAssignationFitness = navyTaskEvaluation.Fitness;
		}
		else if (base.CurrentMainTask != null)
		{
			this.Unassign();
			result = true;
		}
		return result;
	}

	public virtual void AssignCommander(BaseNavyCommander commander)
	{
		this.Commander = commander;
	}

	public override void Release()
	{
		base.Release();
		this.AssignCommander(null);
	}

	protected abstract void FilterTasks();

	private List<NavyTaskEvaluation> taskEvaluations = new List<NavyTaskEvaluation>();

	public enum ArmyRole
	{
		Forteress,
		Renfort,
		TaskForce,
		Land,
		Convoi,
		Bombardier
	}

	public enum ArmyState
	{
		Empty,
		Low,
		Medium,
		High,
		Full
	}
}
