using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class HeroData
{
	public int ChosenSpecialty { get; set; }

	public float CurrentAssignationFitness { get; set; }

	public AssignationData CurrentHeroAssignation { get; set; }

	public float[] CurrentSpecialtyFitness { get; set; }

	public float[] LongTermSpecialtyFitness { get; set; }

	public float WantedAssignationFitness { get; set; }

	public AssignationData WantedHeroAssignation { get; set; }

	public float WantMySpecialtyScore { get; set; }

	public float ComputeFitness(float[] specialtyNeeds)
	{
		float num = 0f;
		int num2 = -1;
		for (int i = 0; i < specialtyNeeds.Length; i++)
		{
			float num3 = specialtyNeeds[i] * this.CurrentSpecialtyFitness[i];
			if (num3 > num)
			{
				num = num3;
				num2 = i;
			}
		}
		if (num2 == this.ChosenSpecialty)
		{
			num = AILayer.Boost(num, this.WantMySpecialtyScore * 0.5f);
		}
		return num;
	}

	public float ComputeFitnessWithSpecialty(AILayer_HeroAssignation.HeroAssignationType specialty)
	{
		int num = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, specialty.ToString());
		if (num >= 0)
		{
			return this.LongTermSpecialtyFitness[num];
		}
		return 0f;
	}

	public void Initialize(Unit hero)
	{
		this.ChosenSpecialty = -1;
		this.constructibleEvaluationAIHelper = AIScheduler.Services.GetService<IConstructibleElementEvaluationAIHelper>();
		this.unitAbilityDatabase = Databases.GetDatabase<UnitAbility>(false);
		this.LongTermSpecialtyFitness = new float[AILayer_HeroAssignation.HeroAssignationTypeNames.Length];
		this.CurrentSpecialtyFitness = new float[this.LongTermSpecialtyFitness.Length];
		this.FindHeroSpecialty(hero);
	}

	public void Update(Unit hero)
	{
		if (this.ChosenSpecialty < 0)
		{
			float num = 0f;
			for (int i = 0; i < this.LongTermSpecialtyFitness.Length; i++)
			{
				if (num < this.LongTermSpecialtyFitness[i])
				{
					num = this.LongTermSpecialtyFitness[i];
					this.ChosenSpecialty = i;
				}
			}
		}
		this.ComputeCurrentHeroSpecialty(hero);
		this.ComputeWantMySpecialtyScore(hero);
		this.WantedHeroAssignation = this.CurrentHeroAssignation;
		this.WantedAssignationFitness = this.CurrentAssignationFitness;
	}

	private void ComputeCurrentHeroSpecialty(Unit hero)
	{
		for (int i = 0; i < this.LongTermSpecialtyFitness.Length; i++)
		{
			this.CurrentSpecialtyFitness[i] = this.LongTermSpecialtyFitness[i];
			if (i == this.ChosenSpecialty)
			{
				this.CurrentSpecialtyFitness[i] = AILayer.Boost(this.CurrentSpecialtyFitness[i], 0.5f);
			}
		}
		float num = hero.GetPropertyValue(SimulationProperties.Health);
		float propertyValue = hero.GetPropertyValue(SimulationProperties.MaximumHealth);
		num /= propertyValue;
		num = Mathf.Clamp01(num);
		this.CurrentSpecialtyFitness[4] = AILayer.Boost(this.CurrentSpecialtyFitness[4], -0.2f * (1f - num));
	}

	private void ComputeWantMySpecialtyScore(Unit hero)
	{
		float num = 20f;
		float propertyValue = hero.GetPropertyValue(SimulationProperties.Level);
		this.WantMySpecialtyScore = propertyValue / num;
		this.WantMySpecialtyScore = Mathf.Clamp01(this.WantMySpecialtyScore);
	}

	private void FindHeroSpecialty(Unit hero)
	{
		float num = 0f;
		List<UnitSkill> list = new List<UnitSkill>();
		DepartmentOfEducation.FillAvailableUnitSkills(hero, ref list);
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(hero))
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Prerequisites != null && list[i].Prerequisites.Length != 0)
				{
					num += 1f;
					float num2 = 1f;
					if (hero.IsSkillUnlocked(list[i].Name))
					{
						int num3 = hero.GetSkillLevel(list[i].Name) + 1;
						num += (float)num3;
						num2 += (float)num3;
					}
					foreach (IAIParameter<InterpreterContext> iaiparameter in this.constructibleEvaluationAIHelper.GetAIParameters(list[i]))
					{
						int num4 = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter.Name.ToString());
						if (num4 >= 0)
						{
							this.LongTermSpecialtyFitness[num4] += iaiparameter.GetValue(interpreterSession.Context) * num2;
						}
					}
				}
			}
			foreach (KeyValuePair<StaticString, int> keyValuePair in hero.EnumerateUnitAbilities())
			{
				float num5 = (float)(keyValuePair.Value + 1);
				bool flag = false;
				UnitAbility element;
				if (this.unitAbilityDatabase.TryGetValue(keyValuePair.Key, out element))
				{
					foreach (IAIParameter<InterpreterContext> iaiparameter2 in this.constructibleEvaluationAIHelper.GetAIParameters(element))
					{
						int num6 = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter2.Name.ToString());
						if (num6 >= 0)
						{
							this.LongTermSpecialtyFitness[num6] += iaiparameter2.GetValue(interpreterSession.Context) * num5;
							flag = true;
						}
					}
				}
				if (flag)
				{
					num += num5;
				}
			}
		}
		for (int j = 0; j < this.LongTermSpecialtyFitness.Length; j++)
		{
			this.LongTermSpecialtyFitness[j] /= num;
		}
	}

	private IConstructibleElementEvaluationAIHelper constructibleEvaluationAIHelper;

	private IDatabase<UnitAbility> unitAbilityDatabase;
}
