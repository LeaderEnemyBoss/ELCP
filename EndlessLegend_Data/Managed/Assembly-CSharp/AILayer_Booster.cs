using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;

public class AILayer_Booster : AILayer
{
	private float GenerateMessagePriorityForFood()
	{
		float num = 0f;
		float num2 = float.MaxValue;
		float propertyValue;
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			propertyValue = this.departmentOfTheInterior.Cities[i].GetPropertyValue(SimulationProperties.Workers);
			if (propertyValue > num)
			{
				num = propertyValue;
			}
			if (propertyValue < num2)
			{
				num2 = propertyValue;
			}
		}
		bool flag = false;
		if ((double)(num2 / num) > 0.8)
		{
			flag = true;
		}
		propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Workers);
		float num3 = 1f - propertyValue / num;
		if (propertyValue == num2 && flag)
		{
			num3 = AILayer.Boost(num3, 0.5f);
		}
		return num3;
	}

	private float GenerateMessageScoreForIndustry()
	{
		float result;
		if (!AILayer_Colonization.IsAbleToColonize(base.AIEntity.Empire))
		{
			result = 1f;
		}
		else
		{
			float num = 0f;
			float num2;
			for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
			{
				num2 = (float)this.departmentOfTheInterior.Cities[i].CityImprovements.Count;
				if (num2 > num)
				{
					num = num2;
				}
			}
			num2 = (float)this.aiEntityCity.City.CityImprovements.Count;
			float num3 = 1f - num2 / num;
			float num4 = 0f;
			if (!this.departmentOfTheTreasury.TryGetNetResourceValue(this.aiEntityCity.City, "Production", out num4, false))
			{
				num4 = 0f;
			}
			if (num4 == 0f)
			{
				num4 = 1f;
			}
			bool flag = false;
			bool flag2 = false;
			float num5 = 0f;
			if (!this.departmentOfTheTreasury.TryGetResourceStockValue(this.aiEntityCity.City, DepartmentOfTheTreasury.Resources.Production, out num5, false))
			{
				num5 = 0f;
			}
			num5 += this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
			num5 = Math.Max(1f, num5);
			for (int j = 0; j < this.constructionQueue.Length; j++)
			{
				Construction construction = this.constructionQueue.PeekAt(j);
				if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(this.aiEntityCity.City, construction.ConstructibleElement, new string[]
				{
					ConstructionFlags.Prerequisite
				}))
				{
					if (!flag2)
					{
						float num6 = 0f;
						for (int k = 0; k < construction.CurrentConstructionStock.Length; k++)
						{
							if (construction.CurrentConstructionStock[k].PropertyName == "Production")
							{
								num6 += construction.CurrentConstructionStock[k].Stock;
								if (construction.IsBuyout)
								{
									num6 = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production");
								}
							}
						}
						float num7 = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production") - num6;
						num5 -= num7;
						if (num5 < 0f && !construction.ConstructibleElementName.ToString().Contains("BoosterGenerator"))
						{
							flag = true;
							flag2 = true;
						}
						if (construction.ConstructibleElementName.ToString().Contains("BoosterGenerator"))
						{
							flag2 = true;
						}
					}
					float num8 = 0f;
					for (int l = 0; l < construction.CurrentConstructionStock.Length; l++)
					{
						if (construction.CurrentConstructionStock[l].PropertyName == "Production")
						{
							num8 += construction.CurrentConstructionStock[l].Stock;
						}
					}
					float num9 = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, construction.ConstructibleElement, "Production") - num8;
					num3 = AILayer.Boost(num3, this.ComputeCostBoost(num9 / num4));
				}
			}
			if (this.aiEntityCity.City.BesiegingEmpire != null)
			{
				num3 = AILayer.Boost(num3, 0.5f);
			}
			if (!flag && !flag2)
			{
				flag = true;
			}
			if (!flag)
			{
				num3 = 0f;
			}
			result = num3;
		}
		return result;
	}

	public float GetPriority(StaticString boosterDefinitionName)
	{
		for (int i = 0; i < this.boosterDefinitionWrappers.Count; i++)
		{
			if (this.boosterDefinitionWrappers[i].BoosterDefinitionName == boosterDefinitionName)
			{
				return this.boosterDefinitionWrappers[i].LastComputedPriority;
			}
		}
		return 0f;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.aiEntityCity = (aiEntity as AIEntity_City);
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfIndustry = base.AIEntity.Empire.GetAgency<DepartmentOfIndustry>();
		this.constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Booster_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Booster_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Booster_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		this.synchronousJobRepository = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		this.InitializeBoosterDefinitionWrapper();
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.departmentOfTheInterior = null;
		this.aiEntityCity = null;
		this.synchronousJobRepository = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		for (int i = this.boosterDefinitionWrappers.Count - 1; i >= 0; i--)
		{
			float num = this.boosterDefinitionWrappers[i].EvaluationFunction();
			this.boosterDefinitionWrappers[i].LastComputedPriority = num;
			CityBoosterNeeds cityBoosterNeeds;
			if (base.AIEntity.AIPlayer.Blackboard.TryGetMessage<CityBoosterNeeds>(this.boosterDefinitionWrappers[i].CurrentMessageId, out cityBoosterNeeds) && cityBoosterNeeds.State == BlackboardMessage.StateValue.Message_None)
			{
				if (cityBoosterNeeds.AvailabilityState == CityBoosterNeeds.CityBoosterState.Cancelled || num == 0f)
				{
					base.AIEntity.AIPlayer.Blackboard.CancelMessage(cityBoosterNeeds);
				}
				else
				{
					cityBoosterNeeds.BoosterPriority = num;
					cityBoosterNeeds.TimeOut = 1;
				}
			}
			else
			{
				this.GenerateEvaluateMessageFor(this.boosterDefinitionWrappers[i]);
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		for (int i = this.boosterDefinitionWrappers.Count - 1; i >= 0; i--)
		{
			CityBoosterNeeds cityBoosterNeeds;
			if (base.AIEntity.AIPlayer.Blackboard.TryGetMessage<CityBoosterNeeds>(this.boosterDefinitionWrappers[i].CurrentMessageId, out cityBoosterNeeds) && cityBoosterNeeds.AvailabilityState == CityBoosterNeeds.CityBoosterState.Available)
			{
				this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartBooster));
				return;
			}
		}
	}

	protected virtual void InitializeBoosterDefinitionWrapper()
	{
		this.boosterDefinitionWrappers.Add(new AILayer_Booster.BoosterDefinitionWrapper("BoosterFood", new Func<float>(this.GenerateMessagePriorityForFood)));
		this.boosterDefinitionWrappers.Add(new AILayer_Booster.BoosterDefinitionWrapper("BoosterIndustry", new Func<float>(this.GenerateMessageScoreForIndustry)));
	}

	private float ComputeCostBoost(float elementTurnDuration)
	{
		elementTurnDuration -= 5f;
		if (elementTurnDuration < 0f)
		{
			elementTurnDuration = 0f;
		}
		float num = elementTurnDuration / this.maximalTurnDuration;
		if (num > 1f)
		{
			num = 1f;
		}
		return this.maximalTurnDurationBoost * num;
	}

	private void GenerateEvaluateMessageFor(AILayer_Booster.BoosterDefinitionWrapper boosterDefinitionWrapper)
	{
		if (boosterDefinitionWrapper.LastComputedPriority == 0f)
		{
			return;
		}
		CityBoosterNeeds cityBoosterNeeds = new CityBoosterNeeds();
		cityBoosterNeeds.CityGuid = this.aiEntityCity.City.GUID;
		cityBoosterNeeds.BoosterPriority = boosterDefinitionWrapper.LastComputedPriority;
		cityBoosterNeeds.BoosterDefinitionName = boosterDefinitionWrapper.BoosterDefinitionName;
		cityBoosterNeeds.TimeOut = 1;
		ulong currentMessageId = base.AIEntity.AIPlayer.Blackboard.AddMessage(cityBoosterNeeds);
		boosterDefinitionWrapper.CurrentMessageId = currentMessageId;
	}

	private SynchronousJobState SynchronousJob_StartBooster()
	{
		SynchronousJobState result;
		if (this.boosterDefinitionWrappers == null || base.AIEntity == null || base.AIEntity.AIPlayer == null)
		{
			result = SynchronousJobState.Failure;
		}
		else
		{
			DepartmentOfScience agency = base.AIEntity.Empire.GetAgency<DepartmentOfScience>();
			bool flag = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>().IsInWarWithSomeone();
			if (agency.GetResearchPropertyValue("UnlockedTechnologyCount") > 16f && agency.GetTechnologyState("TechnologyDefinitionAllBoosterLevel1") != DepartmentOfScience.ConstructibleElement.State.Researched && agency.GetTechnologyState("TechnologyDefinitionAllBoosterLevel2") != DepartmentOfScience.ConstructibleElement.State.Researched && !flag)
			{
				result = SynchronousJobState.Success;
			}
			else
			{
				for (int i = this.boosterDefinitionWrappers.Count - 1; i >= 0; i--)
				{
					CityBoosterNeeds cityBoosterNeeds = null;
					if (this.boosterDefinitionWrappers[i] != null && base.AIEntity.AIPlayer.Blackboard.TryGetMessage<CityBoosterNeeds>(this.boosterDefinitionWrappers[i].CurrentMessageId, out cityBoosterNeeds) && cityBoosterNeeds.AvailabilityState == CityBoosterNeeds.CityBoosterState.Available)
					{
						StaticString x = this.boosterDefinitionWrappers[i].BoosterDefinitionName;
						if (cityBoosterNeeds.BoosterGuid.IsValid && this.departmentOfEducation[cityBoosterNeeds.BoosterGuid] != null && this.departmentOfEducation[cityBoosterNeeds.BoosterGuid].Constructible != null)
						{
							x = this.departmentOfEducation[cityBoosterNeeds.BoosterGuid].Constructible.Name;
						}
						OrderBuyoutAndActivateBooster orderBuyoutAndActivateBooster = new OrderBuyoutAndActivateBooster(base.AIEntity.Empire.Index, x, cityBoosterNeeds.BoosterGuid, false);
						orderBuyoutAndActivateBooster.TargetGUID = this.aiEntityCity.City.GUID;
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderBuyoutAndActivateBooster);
						this.boosterDefinitionWrappers[i].CurrentMessageId = 0UL;
						cityBoosterNeeds.BoosterGuid = 0UL;
						cityBoosterNeeds.AvailabilityState = CityBoosterNeeds.CityBoosterState.Success;
						base.AIEntity.AIPlayer.Blackboard.CancelMessage(cityBoosterNeeds);
					}
				}
				result = SynchronousJobState.Success;
			}
		}
		return result;
	}

	private AIEntity_City aiEntityCity;

	private List<AILayer_Booster.BoosterDefinitionWrapper> boosterDefinitionWrappers = new List<AILayer_Booster.BoosterDefinitionWrapper>();

	private ConstructionQueue constructionQueue;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfIndustry departmentOfIndustry;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	[InfluencedByPersonality]
	[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_City/AILayer_Production/", new object[]
	{

	})]
	private float maximalTurnDuration = 30f;

	[InfluencedByPersonality]
	[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_City/AILayer_Production/", new object[]
	{

	})]
	private float maximalTurnDurationBoost = 0.5f;

	private class BoosterDefinitionWrapper
	{
		public BoosterDefinitionWrapper(StaticString boosterDefinitionName, Func<float> evaluationFunction)
		{
			this.BoosterDefinitionName = boosterDefinitionName;
			this.EvaluationFunction = evaluationFunction;
		}

		public StaticString BoosterDefinitionName { get; set; }

		public ulong CurrentMessageId { get; set; }

		public Func<float> EvaluationFunction { get; set; }

		public float LastComputedPriority { get; set; }
	}
}
