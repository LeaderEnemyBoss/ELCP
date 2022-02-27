using System;
using System.Collections.Generic;
using System.Linq;
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
		this.FindHeroSpecialtyELCP(hero);
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
		Diagnostics.Log("======================================================================================");
		Diagnostics.Log("ELCP {0} FindHeroSpecialty", new object[]
		{
			hero.Name
		});
		float num = 0f;
		List<UnitSkill> list = new List<UnitSkill>();
		DepartmentOfEducation.FillAvailableUnitSkills(hero, ref list);
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(hero))
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Prerequisites != null && list[i].Prerequisites.Length != 0)
				{
					Diagnostics.Log("checking skill {0}, num {1}", new object[]
					{
						list[i].Name,
						num
					});
					num += 1f;
					float num2 = 1f;
					if (hero.IsSkillUnlocked(list[i].Name))
					{
						int num3 = hero.GetSkillLevel(list[i].Name) + 1;
						num += (float)num3;
						num2 += (float)num3;
						Diagnostics.Log("skill unlocked, levelfactor {0}", new object[]
						{
							num2
						});
					}
					foreach (IAIParameter<InterpreterContext> iaiparameter in this.constructibleEvaluationAIHelper.GetAIParameters(list[i]))
					{
						int num4 = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter.Name.ToString());
						if (num4 >= 0)
						{
							this.LongTermSpecialtyFitness[num4] += iaiparameter.GetValue(interpreterSession.Context) * num2;
							Diagnostics.Log("ltspecialiaty {0} + {1} to {2}", new object[]
							{
								num4,
								iaiparameter.GetValue(interpreterSession.Context) * num2,
								this.LongTermSpecialtyFitness[num4]
							});
						}
					}
				}
			}
			foreach (KeyValuePair<StaticString, int> keyValuePair in hero.EnumerateUnitAbilities())
			{
				float num5 = Mathf.Max(1f, (float)(keyValuePair.Value + 1));
				Diagnostics.Log("checking ability {0} {1}/{2}, num {3}", new object[]
				{
					keyValuePair.Key,
					keyValuePair.Value,
					num5,
					num
				});
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
							Diagnostics.Log("ltspecialiaty {0} + {1} to {2}", new object[]
							{
								num6,
								iaiparameter2.GetValue(interpreterSession.Context) * num5,
								this.LongTermSpecialtyFitness[num6]
							});
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
		Diagnostics.Log("+++++++++++++++++++");
		for (int j = 0; j < this.LongTermSpecialtyFitness.Length; j++)
		{
			Diagnostics.Log("final specialty {0}: {1}/{2} = {3}", new object[]
			{
				j,
				this.LongTermSpecialtyFitness[j],
				num,
				this.LongTermSpecialtyFitness[j] / num
			});
			this.LongTermSpecialtyFitness[j] /= num;
		}
	}

	private void FindHeroSpecialtyELCP(Unit hero)
	{
		List<UnitSkill> list = new List<UnitSkill>();
		DepartmentOfEducation.FillAvailableUnitSkills(hero, ref list);
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(hero))
		{
			for (int i = 0; i < list.Count; i++)
			{
				if (list[i].Prerequisites != null && list[i].Prerequisites.Length != 0 && hero.IsSkillUnlocked(list[i].Name))
				{
					int num = hero.GetSkillLevel(list[i].Name) + 1;
					foreach (IAIParameter<InterpreterContext> iaiparameter in this.constructibleEvaluationAIHelper.GetAIParameters(list[i]))
					{
						int num2 = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter.Name.ToString());
						if (num2 >= 0)
						{
							for (int j = 0; j < num; j++)
							{
								float num3 = iaiparameter.GetValue(interpreterSession.Context);
								if (j > 0)
								{
									num3 *= 0.5f;
								}
								this.LongTermSpecialtyFitness[num2] = AILayer.Boost(this.LongTermSpecialtyFitness[num2], num3);
							}
						}
					}
				}
			}
			List<UnitAbilityReference> list2 = new List<UnitAbilityReference>();
			if (hero.UnitDesign.UnitBodyDefinition.UnitAbilities != null)
			{
				list2.AddRange(hero.UnitDesign.UnitBodyDefinition.UnitAbilities.ToList<UnitAbilityReference>());
			}
			if ((hero.UnitDesign as UnitProfile).ProfileAbilityReferences != null)
			{
				list2.AddRange((hero.UnitDesign as UnitProfile).ProfileAbilityReferences.ToList<UnitAbilityReference>());
			}
			list2.Add(new UnitAbilityReference(UnitAbility.ReadonlyLastStand, 0));
			using (IEnumerator<KeyValuePair<StaticString, int>> enumerator2 = hero.EnumerateUnitAbilities().GetEnumerator())
			{
				while (enumerator2.MoveNext())
				{
					KeyValuePair<StaticString, int> keyValuePair = enumerator2.Current;
					int num4 = list2.FindIndex((UnitAbilityReference r) => r.Name == keyValuePair.Key);
					if (num4 >= 0)
					{
						float a = Mathf.Max(1f, (float)(list2[num4].Level + 1));
						float b = Mathf.Max(1f, (float)(keyValuePair.Value + 1));
						float num5 = Mathf.Min(a, b);
						UnitAbility element;
						if (this.unitAbilityDatabase.TryGetValue(keyValuePair.Key, out element))
						{
							foreach (IAIParameter<InterpreterContext> iaiparameter2 in this.constructibleEvaluationAIHelper.GetAIParameters(element))
							{
								int num6 = Array.IndexOf<string>(AILayer_HeroAssignation.HeroAssignationTypeNames, iaiparameter2.Name.ToString());
								if (num6 >= 0)
								{
									int num7 = 0;
									while ((float)num7 < num5)
									{
										float num8 = iaiparameter2.GetValue(interpreterSession.Context);
										if (num7 > 0)
										{
											num8 *= 0.5f;
										}
										this.LongTermSpecialtyFitness[num6] = AILayer.Boost(this.LongTermSpecialtyFitness[num6], num8);
										num7++;
									}
								}
							}
						}
					}
				}
			}
		}
	}

	private IConstructibleElementEvaluationAIHelper constructibleEvaluationAIHelper;

	private IDatabase<UnitAbility> unitAbilityDatabase;
}
