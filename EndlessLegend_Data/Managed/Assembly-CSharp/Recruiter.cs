using System;
using System.Collections.Generic;
using Amplitude;
using UnityEngine;

public class Recruiter
{
	public Recruiter()
	{
		this.InitialProductionPriority = new HeuristicValue(0f);
		this.WantedUnitCount = new HeuristicValue(0f);
	}

	public bool Foldout { get; set; }

	public bool UnitProductionFoldout { get; set; }

	public AIEntity AIEntity { get; set; }

	public Func<UnitDesign, bool> UnitDesignFilter { get; set; }

	public float AverageMilitaryPower
	{
		get
		{
			return this.averageMilitaryPower;
		}
	}

	public HeuristicValue InitialProductionPriority { get; private set; }

	public HeuristicValue WantedUnitCount { get; set; }

	public float CurrentUnitCount { get; set; }

	public void Initialize()
	{
		if (this.AIEntity.Empire is MajorEmpire)
		{
			this.VictoryLayer = this.AIEntity.GetLayer<AILayer_Victory>();
		}
		this.ComputeMilitaryBodyRatio();
	}

	public void Produce(float currentUnitCount, float recruiterProductionPercent)
	{
		this.CurrentUnitCount = currentUnitCount;
		float currentMilitaryPower = this.AverageMilitaryPower * currentUnitCount;
		this.needMilitaryPower = this.AverageMilitaryPower * this.WantedUnitCount;
		this.ResetProductionPriority(currentMilitaryPower);
		this.InitialProductionPriority.Boost(recruiterProductionPercent - 0.5f, "Based on navy importance.", new object[0]);
		this.maxNewUnits = Mathf.Max(2f, currentUnitCount * 2f);
		this.currentProductionPriority = this.InitialProductionPriority;
		this.DoProduce();
	}

	public void FillUnitProduction(ref List<EvaluableMessage_UnitProduction> requestUnitMessages)
	{
		this.AIEntity.AIPlayer.Blackboard.FillMessages<EvaluableMessage_UnitProduction>(BlackboardLayerID.Empire, (EvaluableMessage_UnitProduction match) => match.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel && match.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained && (match.UnitDesign == null || this.availableMilitaryBody.Contains(match.UnitDesign.UnitBodyDefinition.Name)), ref requestUnitMessages);
	}

	private void DoProduce()
	{
		this.ComputeMilitaryBodyRatio();
		List<EvaluableMessage_UnitProduction> list = new List<EvaluableMessage_UnitProduction>();
		this.FillUnitProduction(ref list);
		HeuristicValue globalMotivation = new HeuristicValue(0.9f);
		for (int i = 0; i < list.Count; i++)
		{
			if (!this.CheckUnitProduction(list[i]))
			{
				list[i].Cancel();
			}
			else
			{
				HeuristicValue unitPriority = this.GetUnitPriority(list[i].UnitDesign);
				list[i].Refresh(globalMotivation, unitPriority);
				this.ProcessUnitProduction(list[i].UnitDesign);
			}
		}
		while (this.needMilitaryPower > 0f)
		{
			UnitDesign unitDesign = this.ChooseMilitaryUnitToBuild();
			if (unitDesign == null)
			{
				break;
			}
			HeuristicValue unitPriority2 = this.GetUnitPriority(unitDesign);
			EvaluableMessage_UnitProduction message = new EvaluableMessage_UnitProduction(globalMotivation, unitPriority2, unitDesign, -1, 1, AILayer_AccountManager.MilitaryAccountName);
			this.AIEntity.AIPlayer.Blackboard.AddMessage(message);
			this.ProcessUnitProduction(unitDesign);
		}
	}

	private void ComputeMilitaryBodyRatio()
	{
		this.overralBodyRatio = 1f;
		this.overralUnitCount = 1f;
		this.bodyCount.Clear();
		this.availableMilitaryDesign.Clear();
		this.wantedBodyRatio.Clear();
		this.availableMilitaryBody.Clear();
		this.averageMilitaryPower = 0f;
		IAIUnitDesignDataRepository service = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
		DepartmentOfDefense agency = this.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.UnitDesignDatabase.UserDefinedUnitDesigns.Count; i++)
		{
			UnitDesign unitDesign = agency.UnitDesignDatabase.UserDefinedUnitDesigns[i];
			if (this.UnitDesignFilter(unitDesign))
			{
				AIUnitDesignData aiunitDesignData;
				if (service.TryGetUnitDesignData(this.AIEntity.Empire.Index, unitDesign.Model, out aiunitDesignData))
				{
					this.availableMilitaryDesign.Add(unitDesign);
					if (unitDesign.Context != null)
					{
						this.averageMilitaryPower += unitDesign.Context.GetPropertyValue(SimulationProperties.MilitaryPower);
					}
					int num = this.availableMilitaryBody.IndexOf(aiunitDesignData.BodyName);
					if (num < 0)
					{
						num = this.availableMilitaryBody.Count;
						this.availableMilitaryBody.Add(aiunitDesignData.BodyName);
						this.bodyCount.Add(aiunitDesignData.EmpireBodyCount);
						this.overralUnitCount += aiunitDesignData.EmpireBodyCount;
						float bodyWeight = this.GetBodyWeight(unitDesign);
						this.wantedBodyRatio.Add(bodyWeight);
						this.overralBodyRatio += bodyWeight;
					}
				}
			}
		}
		if (this.availableMilitaryDesign.Count > 0)
		{
			this.averageMilitaryPower /= (float)this.availableMilitaryDesign.Count;
		}
		for (int j = 0; j < this.wantedBodyRatio.Count; j++)
		{
			List<float> list2;
			List<float> list = list2 = this.wantedBodyRatio;
			int index2;
			int index = index2 = j;
			float num2 = list2[index2];
			list[index] = num2 / this.overralBodyRatio;
		}
	}

	private UnitDesign ChooseMilitaryUnitToBuild()
	{
		this.availableMilitaryDesign.Sort(delegate(UnitDesign left, UnitDesign right)
		{
			float designRatio = this.GetDesignRatio(left.UnitBodyDefinition.Name);
			float designRatio2 = this.GetDesignRatio(right.UnitBodyDefinition.Name);
			return -1 * designRatio.CompareTo(designRatio2);
		});
		for (int i = 0; i < this.availableMilitaryDesign.Count; i++)
		{
			if (this.MayBuildIt(this.availableMilitaryDesign[i]))
			{
				return this.availableMilitaryDesign[i];
			}
		}
		return null;
	}

	private float GetDesignRatio(StaticString bodyName)
	{
		int num = this.availableMilitaryBody.IndexOf(bodyName);
		if (num >= 0)
		{
			float num2 = this.bodyCount[num] / this.overralUnitCount;
			return this.wantedBodyRatio[num] - num2;
		}
		return -2.14748365E+09f;
	}

	private bool MayBuildIt(UnitDesign unitDesign)
	{
		return true;
	}

	private bool CheckUnitProduction(EvaluableMessage_UnitProduction unitRequest)
	{
		if (unitRequest.UnitDesign == null)
		{
			return false;
		}
		if (unitRequest.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
		{
			if (this.needMilitaryPower <= 0f)
			{
				return false;
			}
			if (this.maxNewUnits <= 0f)
			{
				return false;
			}
		}
		return true;
	}

	private void ProcessUnitProduction(UnitDesign unitDesign)
	{
		int num = this.availableMilitaryBody.IndexOf(unitDesign.UnitBodyDefinition.Name);
		if (num >= 0)
		{
			this.overralUnitCount += 1f;
			List<float> list2;
			List<float> list = list2 = this.bodyCount;
			int index2;
			int index = index2 = num;
			float num2 = list2[index2];
			list[index] = num2 + 1f;
		}
		if (unitDesign.Context != null && unitDesign.Context.SimulationObject != null)
		{
			this.needMilitaryPower -= unitDesign.Context.GetPropertyValue(SimulationProperties.MilitaryPower);
		}
		else
		{
			this.needMilitaryPower -= this.averageMilitaryPower;
		}
		this.maxNewUnits -= 1f;
		this.currentProductionPriority = AILayer.Boost(this.currentProductionPriority, -0.1f);
	}

	private float GetBodyWeight(UnitDesign unitDesign)
	{
		if (unitDesign.CheckAgainstTag("UnitClassInfantry"))
		{
			return 20f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassArcher"))
		{
			return 10f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassSupport"))
		{
			return 5f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassFrigate"))
		{
			return 20f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassInterceptor"))
		{
			return 10f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassJuggernaut"))
		{
			return 5f;
		}
		if (unitDesign.CheckAgainstTag("UnitClassSubmersible"))
		{
			return 5f;
		}
		return 10f;
	}

	private void ResetProductionPriority(float currentMilitaryPower)
	{
		this.InitialProductionPriority.Reset();
		if (this.needMilitaryPower < currentMilitaryPower)
		{
			float num = 2f;
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add(currentMilitaryPower, "Current military power", new object[0]);
			heuristicValue.Divide(this.needMilitaryPower, "Wanted military power", new object[0]);
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add(heuristicValue, "Ratio", new object[0]);
			heuristicValue2.Clamp(0f, num);
			heuristicValue2.Divide(num, "Normalize", new object[0]);
			heuristicValue2.Multiply(0.7f, "Max difference", new object[0]);
			this.InitialProductionPriority.Add(0.8f, "Max priority when over wanted", new object[0]);
			this.InitialProductionPriority.Subtract(heuristicValue2, "Current higher than wanted", new object[0]);
		}
		else if (currentMilitaryPower > 0f)
		{
			float num2 = 4f;
			HeuristicValue heuristicValue3 = new HeuristicValue(0f);
			heuristicValue3.Add(this.needMilitaryPower, "Wanted military power", new object[0]);
			heuristicValue3.Divide(currentMilitaryPower, "Current military power", new object[0]);
			HeuristicValue heuristicValue4 = new HeuristicValue(0f);
			heuristicValue4.Add(heuristicValue3, "Ratio", new object[0]);
			heuristicValue4.Clamp(0f, num2);
			heuristicValue4.Divide(num2, "Normalize", new object[0]);
			heuristicValue4.Multiply(0.6f, "Max difference", new object[0]);
			this.InitialProductionPriority.Add(0.3f, "Minimal priority", new object[0]);
			this.InitialProductionPriority.Add(heuristicValue4, "Current is under needed", new object[0]);
		}
		else
		{
			this.InitialProductionPriority.Add(0.8f, "current == 0, max priority!", new object[0]);
		}
	}

	private HeuristicValue GetUnitPriority(UnitDesign unitDesign)
	{
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		heuristicValue.Add(this.currentProductionPriority, "Current unit priority", new object[0]);
		HeuristicValue heuristicValue2 = new HeuristicValue(0f);
		int num = this.availableMilitaryBody.IndexOf(unitDesign.UnitBodyDefinition.Name);
		if (num >= 0)
		{
			float num2 = this.bodyCount[num] / this.overralUnitCount;
			float num3 = this.wantedBodyRatio[num];
			if (num2 > num3)
			{
				heuristicValue2.Log("Too much of this body already", new object[0]);
				heuristicValue2.Add(num2, "Current ratio", new object[0]);
				heuristicValue2.Divide(num3, "Wanted ratio", new object[0]);
				heuristicValue2.Clamp(0f, 2f);
				heuristicValue2.Multiply(-1f, "invert", new object[0]);
			}
			else
			{
				heuristicValue2.Log("Not enough of this body for now.", new object[0]);
				heuristicValue2.Add(num3, "Wanted ratio", new object[0]);
				heuristicValue2.Divide(num2, "Current ratio", new object[0]);
				heuristicValue2.Clamp(0f, 2f);
			}
		}
		heuristicValue2.Multiply(0.05f, "constant", new object[0]);
		heuristicValue.Boost(heuristicValue2, "Body ratio boost", new object[0]);
		DepartmentOfTheInterior agency = this.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		if (unitDesign.Name.ToString().Contains("Preacher") && agency.AssimilatedFactions.Count > 0 && (this.VictoryLayer == null || !this.VictoryLayer.NeedPreachers))
		{
			heuristicValue.Boost(-0.4f, "Bad Unit Malus", new object[0]);
		}
		if (unitDesign.Name.ToString().Contains("EyelessOnesCaecator") || unitDesign.Name.ToString().Contains("CeratanDrider"))
		{
			heuristicValue.Boost(-0.3f, "Bad Unit Malus", new object[0]);
		}
		if (unitDesign.Name.ToString().Contains("Mastermind") && agency.AssimilatedFactions.Count > 0)
		{
			foreach (Faction faction in agency.AssimilatedFactions)
			{
				if (faction.Name != "Ceratan" && faction.Name != "EyelessOnes")
				{
					heuristicValue.Boost(-0.2f, "Bad Unit Malus", new object[0]);
					break;
				}
			}
		}
		return heuristicValue;
	}

	private List<UnitDesign> availableMilitaryDesign = new List<UnitDesign>();

	private List<StaticString> availableMilitaryBody = new List<StaticString>();

	private List<float> bodyCount = new List<float>();

	private List<float> wantedBodyRatio = new List<float>();

	private float overralBodyRatio;

	private float overralUnitCount;

	private float currentProductionPriority;

	private float needMilitaryPower;

	private float maxNewUnits;

	private float averageMilitaryPower;

	private AILayer_Victory VictoryLayer;
}
