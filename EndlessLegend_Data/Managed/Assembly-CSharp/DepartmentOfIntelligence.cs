using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[Diagnostics.TagAttribute("Agency")]
[Diagnostics.TagAttribute("Agency")]
[OrderProcessor(typeof(OrderRoundUp), "RoundUp")]
[OrderProcessor(typeof(OrderRevealInfiltratedSpiesByInfiltration), "RevealInfiltratedSpiesByInfiltration")]
[OrderProcessor(typeof(OrderStartLeechByInfiltration), "StartLeechByInfiltration")]
[OrderProcessor(typeof(OrderToggleInfiltration), "ToggleInfiltration")]
[OrderProcessor(typeof(OrderDamageGovernorByInfiltration), "DamageGovernorByInfiltration")]
[OrderProcessor(typeof(OrderAntiSpyCheck), "AntiSpyCheck")]
[OrderProcessor(typeof(OrderDamageFortificationByInfiltration), "DamageFortificationByInfiltration")]
public class DepartmentOfIntelligence : Agency, IXmlSerializable
{
	public DepartmentOfIntelligence(global::Empire empire) : base(empire)
	{
	}

	public event EventHandler<InfiltrationEventArgs> InfiltrationLevelChange;

	public event EventHandler<InfiltrationEventArgs> InfiltrationStateChange;

	public event EventHandler<InfiltrationEventArgs> InfiltrationSeniorityChange;

	public event CollectionChangeEventHandler InfiltrationProcessCollectionChange;

	public static float ComputeInfiltrateLevelInInfiltratePointStock(SimulationObject context, float wantedLevel)
	{
		if (DepartmentOfIntelligence.infiltrateLevelInterpreterContext == null)
		{
			DepartmentOfIntelligence.infiltrateLevelInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfIntelligence/InfiltrateLevelFormula");
			DepartmentOfIntelligence.infiltrateLevelFormulaTokens = Interpreter.InfixTransform(value);
		}
		InterpreterContext obj = DepartmentOfIntelligence.infiltrateLevelInterpreterContext;
		float result;
		lock (obj)
		{
			DepartmentOfIntelligence.infiltrateLevelInterpreterContext.SimulationObject = DepartmentOfIntelligence.empireSimulationPath.GetFirstValidatedObject(context);
			DepartmentOfIntelligence.infiltrateLevelInterpreterContext.Register("WantedLevel", wantedLevel);
			result = (float)Interpreter.Execute(DepartmentOfIntelligence.infiltrateLevelFormulaTokens, DepartmentOfIntelligence.infiltrateLevelInterpreterContext);
		}
		return result;
	}

	public static float ComputeInfiltrationCost(Amplitude.Unity.Game.Empire infiltratingEmpire, IGarrison target)
	{
		if (DepartmentOfIntelligence.infiltrationCostInterpreterContext == null)
		{
			DepartmentOfIntelligence.infiltrationCostInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfIntelligence/InfiltrationCostFormula");
			DepartmentOfIntelligence.infiltrationCostFormulaTokens = Interpreter.InfixTransform(value);
		}
		InterpreterContext obj = DepartmentOfIntelligence.infiltrationCostInterpreterContext;
		float result;
		lock (obj)
		{
			DepartmentOfIntelligence.infiltrationCostInterpreterContext.SimulationObject = DepartmentOfIntelligence.empireSimulationPath.GetFirstValidatedObject(infiltratingEmpire);
			DepartmentOfIntelligence.infiltrationCostInterpreterContext.Register("Target", target);
			DepartmentOfIntelligence.infiltrationCostInterpreterContext.Register("InfiltratingEmpire", infiltratingEmpire);
			result = (float)Interpreter.Execute(DepartmentOfIntelligence.infiltrationCostFormulaTokens, DepartmentOfIntelligence.infiltrationCostInterpreterContext);
		}
		return result;
	}

	public static float ComputeInfiltrationSucceedExperienceGain(Amplitude.Unity.Game.Empire infiltratingEmpire, IGarrison target, Unit hero)
	{
		if (DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext == null)
		{
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfIntelligence/InfiltrationSucceedExperienceGain");
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainFormulaTokens = Interpreter.InfixTransform(value);
		}
		InterpreterContext obj = DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext;
		float result;
		lock (obj)
		{
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext.SimulationObject = DepartmentOfIntelligence.empireSimulationPath.GetFirstValidatedObject(infiltratingEmpire);
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext.Register("Target", target);
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext.Register("Hero", hero);
			DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext.Register("InfiltratingEmpire", infiltratingEmpire);
			result = (float)Interpreter.Execute(DepartmentOfIntelligence.infiltrationSucceedExperienceGainFormulaTokens, DepartmentOfIntelligence.infiltrationSucceedExperienceGainInterpreterContext);
		}
		return result;
	}

	public static float ComputeInfiltrationActionSeniority(Amplitude.Unity.Game.Empire infiltratingEmpire, Unit hero, InfiltrationAction infiltrationAction)
	{
		if (DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext == null)
		{
			DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext = new InterpreterContext(null);
			string value = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<string>("Gameplay/Agencies/DepartmentOfIntelligence/InfiltrationActionSeniorityFormula");
			DepartmentOfIntelligence.infiltrationActionSeniorityFormulaTokens = Interpreter.InfixTransform(value);
		}
		InterpreterContext obj = DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext;
		float result;
		lock (obj)
		{
			DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext.SimulationObject = DepartmentOfIntelligence.empireSimulationPath.GetFirstValidatedObject(infiltratingEmpire);
			DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext.Register("Hero", hero);
			DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext.Register("InfiltratingEmpire", infiltratingEmpire);
			DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext.Register("ActionLevel", (float)infiltrationAction.Level);
			result = (float)Interpreter.Execute(DepartmentOfIntelligence.infiltrationActionSeniorityFormulaTokens, DepartmentOfIntelligence.infiltrationActionSeniorityGainInterpreterContext);
		}
		return result;
	}

	public static float GetCityAntiSpyValue(City city)
	{
		float result = -1f;
		if (city != null)
		{
			result = city.GetPropertyValue(SimulationProperties.NetCityAntiSpy);
		}
		return result;
	}

	public static DepartmentOfIntelligence.AntiSpyResult GetAntiSpyResult(City city, out float parameter)
	{
		parameter = 0f;
		if (city == null)
		{
			Diagnostics.LogError("City is null.");
			return DepartmentOfIntelligence.AntiSpyResult.Nothing;
		}
		float cityAntiSpyValue = DepartmentOfIntelligence.GetCityAntiSpyValue(city);
		return DepartmentOfIntelligence.GetAntiSpyResult(cityAntiSpyValue, out parameter);
	}

	public static float GetAntiSpyResultProbability(City city, DepartmentOfIntelligence.AntiSpyResult antiSpyResult)
	{
		float cityAntiSpyValue = DepartmentOfIntelligence.GetCityAntiSpyValue(city);
		float[] array = new float[DepartmentOfIntelligence.antiSpyResults.Length];
		float num = 0f;
		DepartmentOfIntelligence.FillAntiSpyPercentArray(cityAntiSpyValue, out array, out num);
		return array[(int)antiSpyResult] / num;
	}

	public static DepartmentOfIntelligence.AntiSpyResult GetAntiSpyResult(float cityAntiSpyValue, out float parameter)
	{
		parameter = 0f;
		if (cityAntiSpyValue < 0f)
		{
			Diagnostics.LogError("Fail getting AntiSpyLevel.");
			return DepartmentOfIntelligence.AntiSpyResult.Nothing;
		}
		float[] array = new float[DepartmentOfIntelligence.antiSpyResults.Length];
		float max = 0f;
		DepartmentOfIntelligence.FillAntiSpyPercentArray(cityAntiSpyValue, out array, out max);
		float num = UnityEngine.Random.Range(0f, max);
		for (int i = 0; i < DepartmentOfIntelligence.antiSpyResults.Length; i++)
		{
			num -= array[i];
			if (num <= 0f)
			{
				DepartmentOfIntelligence.AntiSpyResult antiSpyResult = DepartmentOfIntelligence.antiSpyResults[i];
				if (antiSpyResult == DepartmentOfIntelligence.AntiSpyResult.Wounded)
				{
					parameter = Amplitude.Unity.Runtime.Runtime.Registry.GetValue<float>("Gameplay/Ancillaries/AntiSpy/WoundedPercentageValue");
				}
				return antiSpyResult;
			}
		}
		return DepartmentOfIntelligence.AntiSpyResult.Nothing;
	}

	public static void FillAntiSpyPercentArray(float antiSpyValue, out float[] antiSpyPercentArray, out float antiSpyArraySum)
	{
		antiSpyPercentArray = new float[DepartmentOfIntelligence.antiSpyResults.Length];
		antiSpyArraySum = 0f;
		if (DepartmentOfIntelligence.antiSpyCurves == null)
		{
			IDatabase<Amplitude.Unity.Framework.AnimationCurve> database = Databases.GetDatabase<Amplitude.Unity.Framework.AnimationCurve>(false);
			DepartmentOfIntelligence.antiSpyCurves = new Amplitude.Unity.Framework.AnimationCurve[DepartmentOfIntelligence.antiSpyResults.Length];
			for (int i = 0; i < DepartmentOfIntelligence.antiSpyResults.Length; i++)
			{
				database.TryGetValue("AnimationCurve" + Enum.GetName(typeof(DepartmentOfIntelligence.AntiSpyResult), DepartmentOfIntelligence.antiSpyResults[i]), out DepartmentOfIntelligence.antiSpyCurves[i]);
			}
		}
		for (int j = 0; j < DepartmentOfIntelligence.antiSpyResults.Length; j++)
		{
			if (DepartmentOfIntelligence.antiSpyCurves[j] != null)
			{
				antiSpyPercentArray[j] = DepartmentOfIntelligence.antiSpyCurves[j].Evaluate(antiSpyValue);
				antiSpyArraySum += antiSpyPercentArray[j];
			}
		}
	}

	public static DepartmentOfIntelligence.AntiSpyResult[] GetAntiSpyResults()
	{
		return DepartmentOfIntelligence.antiSpyResults;
	}

	public static IEnumerable<SpiedGarrison> GetCityInfiltratedSpies(GameEntityGUID infiltratedCityGUID)
	{
		IGameService gameService = Services.GetService<IGameService>();
		global::Game game = gameService.Game as global::Game;
		global::Empire[] empires = game.Empires;
		if (empires == null)
		{
			yield break;
		}
		for (int index = 0; index < empires.Length; index++)
		{
			DepartmentOfIntelligence departmentOfIntelligence = empires[index].GetAgency<DepartmentOfIntelligence>();
			if (departmentOfIntelligence != null)
			{
				for (int spiedGarrisonIndex = departmentOfIntelligence.SpiedGarrisons.Count - 1; spiedGarrisonIndex >= 0; spiedGarrisonIndex--)
				{
					if (departmentOfIntelligence.SpiedGarrisons[spiedGarrisonIndex].GUID == infiltratedCityGUID)
					{
						yield return departmentOfIntelligence.SpiedGarrisons[spiedGarrisonIndex];
						break;
					}
				}
			}
		}
		yield break;
	}

	public static bool IsHeroInfiltrating(Unit hero, Amplitude.Unity.Game.Empire heroOwner)
	{
		DepartmentOfIntelligence agency = heroOwner.GetAgency<DepartmentOfIntelligence>();
		if (agency == null)
		{
			return false;
		}
		for (int i = 0; i < agency.infiltrationProcesses.Count; i++)
		{
			InfiltrationProcessus infiltrationProcessus = agency.infiltrationProcesses[i];
			if (infiltrationProcessus.HeroGuid == hero.GUID)
			{
				return infiltrationProcessus.SpyState == InfiltrationProcessus.InfiltrationState.OnGoing;
			}
		}
		return false;
	}

	public static bool IsHeroInfiltrating(Unit hero)
	{
		float propertyValue = hero.GetPropertyValue(SimulationProperties.InfiltrationCooldown);
		float propertyValue2 = hero.GetPropertyValue(SimulationProperties.MaximumInfiltrationCooldown);
		return propertyValue < propertyValue2;
	}

	public static int InfiltrationRemainingTurns(Unit hero)
	{
		float propertyValue = hero.GetPropertyValue(SimulationProperties.InfiltrationCooldown);
		float propertyValue2 = hero.GetPropertyValue(SimulationProperties.MaximumInfiltrationCooldown);
		return Mathf.Max(Mathf.RoundToInt(propertyValue2 - propertyValue), 1);
	}

	public static bool IsHeroAlreadyUnderInfiltrationProcessus(GameEntityGUID heroGuid, Amplitude.Unity.Game.Empire heroOwner)
	{
		DepartmentOfIntelligence agency = heroOwner.GetAgency<DepartmentOfIntelligence>();
		return agency != null && agency.infiltrationProcesses.Exists((InfiltrationProcessus match) => match.HeroGuid == heroGuid);
	}

	public static bool IsGarrisonAlreadyUnderInfiltrationProcessus(GameEntityGUID garrisonGuidToSpy, Amplitude.Unity.Game.Empire heroOwner)
	{
		DepartmentOfIntelligence agency = heroOwner.GetAgency<DepartmentOfIntelligence>();
		return agency != null && agency.infiltrationProcesses.Exists((InfiltrationProcessus match) => match.GarrisonGuid == garrisonGuidToSpy);
	}

	public bool CanBeInfiltrate(IGarrison target, out float cost, List<StaticString> failures)
	{
		cost = 0f;
		cost = DepartmentOfIntelligence.ComputeInfiltrationCost(base.Empire, target);
		if (!this.departmentOfTheTreasury.CanAfford(cost, DepartmentOfTheTreasury.Resources.InfiltrationCost))
		{
			if (failures != null)
			{
				failures.Add(DepartmentOfIntelligence.InfiltrationTargetFailureNotAffordable);
			}
			return false;
		}
		if (target is City)
		{
			City city = target as City;
			if (city != null && city.BesiegingEmpire != null)
			{
				bool flag = false;
				if (city.BesiegingEmpire is MinorEmpire)
				{
					flag = true;
				}
				else
				{
					for (int i = 0; i < this.departmentOfForeignAffairs.DiplomaticRelations.Count; i++)
					{
						DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[i];
						if (city.BesiegingEmpire.Index == diplomaticRelation.OtherEmpireIndex && base.Empire.Index != diplomaticRelation.OtherEmpireIndex && diplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance)
						{
							flag = true;
							break;
						}
					}
				}
				if (city.BesiegingEmpire != base.Empire && flag)
				{
					if (failures != null)
					{
						failures.Add(DepartmentOfIntelligence.InfiltrationTargetFailureSiege);
					}
					return false;
				}
			}
			IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
			if (service != null && service.IsShared(DownloadableContent20.ReadOnlyName) && city.IsInfected)
			{
				if (failures != null)
				{
					failures.Add(DepartmentOfIntelligence.InfiltrationTargetFailureCityInfected);
				}
				return false;
			}
		}
		if (!this.IsGarrisonVisible(target))
		{
			if (failures != null)
			{
				failures.Add(DepartmentOfIntelligence.InfiltrationTargetFailureNotVisible);
			}
			return false;
		}
		if (DepartmentOfIntelligence.IsGarrisonAlreadyUnderInfiltrationProcessus(target.GUID, base.Empire))
		{
			if (failures != null)
			{
				failures.Add(DepartmentOfIntelligence.InfiltrationTargetFailureAlreadyInfiltrated);
			}
			return false;
		}
		return true;
	}

	public int ComputeNumberOfTurnBeforeNextInfiltrationLevel(SpiedGarrison spiedGarrison)
	{
		float num;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(spiedGarrison, DepartmentOfTheTreasury.Resources.InfiltrationPoint, out num, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.InfiltrationPoint,
				spiedGarrison.GUID
			});
			return 0;
		}
		float propertyValue = spiedGarrison.GetPropertyValue(SimulationProperties.InfiltrateLevel);
		float propertyValue2 = spiedGarrison.GetPropertyValue(SimulationProperties.MaximumInfiltrateLevel);
		if (propertyValue >= propertyValue2)
		{
			return int.MaxValue;
		}
		float num2 = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, propertyValue + 1f);
		float propertyValue3 = spiedGarrison.GetPropertyValue(SimulationProperties.NetInfiltrationPoint);
		if (propertyValue3 == 0f)
		{
			return int.MaxValue;
		}
		return Mathf.CeilToInt((num2 - num) / propertyValue3);
	}

	public int GetHeroNumberOfTurnBeforeInfiltrationLevel(Unit hero)
	{
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			if (this.spiedGarrisons[i].Hero != null && this.spiedGarrisons[i].Hero.GUID == hero.GUID)
			{
				return this.ComputeNumberOfTurnBeforeNextInfiltrationLevel(this.spiedGarrisons[i]);
			}
		}
		return 0;
	}

	public void GetHeroinfiltrationLevel(Unit hero, out int currentLevel, out int maximumLevel)
	{
		currentLevel = 0;
		maximumLevel = 0;
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			if (this.spiedGarrisons[i].Hero != null && this.spiedGarrisons[i].Hero.GUID == hero.GUID)
			{
				currentLevel = Mathf.RoundToInt(this.spiedGarrisons[i].GetPropertyValue(SimulationProperties.InfiltrateLevel));
				maximumLevel = Mathf.RoundToInt(this.spiedGarrisons[i].GetPropertyValue(SimulationProperties.MaximumInfiltrateLevel));
			}
		}
	}

	public bool CanInfiltrate(Unit hero, IGarrison target, bool fromGround, out float cost, bool silent = true)
	{
		cost = 0f;
		if (!hero.CheckUnitAbility(UnitAbility.ReadonlySpy, -1))
		{
			return false;
		}
		if (DepartmentOfEducation.IsInjured(hero) || DepartmentOfEducation.IsLocked(hero) || DepartmentOfEducation.CheckGarrisonAgainstSiege(hero, hero.Garrison))
		{
			return false;
		}
		if (target.Empire == base.Empire)
		{
			return false;
		}
		if (target is City)
		{
			City city = target as City;
			if (city.BesiegingEmpire != null && city.BesiegingEmpire != base.Empire)
			{
				return false;
			}
		}
		if (hero.Garrison != null && hero.Garrison.IsInEncounter)
		{
			return false;
		}
		if (!this.IsGarrisonVisible(target))
		{
			return false;
		}
		if (DepartmentOfIntelligence.IsGarrisonAlreadyUnderInfiltrationProcessus(target.GUID, base.Empire))
		{
			return false;
		}
		int i = 0;
		while (i < this.InfiltrationProcesses.Count)
		{
			if (this.InfiltrationProcesses[i].HeroGuid == hero.GUID)
			{
				if (this.InfiltrationProcesses[i].SpyState == InfiltrationProcessus.InfiltrationState.OnGoing)
				{
					return false;
				}
				break;
			}
			else
			{
				i++;
			}
		}
		if (fromGround)
		{
			if (!this.IsHeroNearGarrison(hero, target))
			{
				return false;
			}
		}
		else
		{
			cost = DepartmentOfIntelligence.ComputeInfiltrationCost(base.Empire, target);
			if (!this.departmentOfTheTreasury.CanAfford(cost, DepartmentOfTheTreasury.Resources.InfiltrationCost))
			{
				cost = -1f;
				return false;
			}
		}
		return true;
	}

	public bool CanUnassignSpy(Unit hero)
	{
		int i = 0;
		while (i < this.infiltrationProcesses.Count)
		{
			if (this.infiltrationProcesses[i].HeroGuid == hero.GUID)
			{
				if (this.infiltrationProcesses[i].SpyState == InfiltrationProcessus.InfiltrationState.OnGoing)
				{
					return true;
				}
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].GarrisonGuid, out gameEntity))
				{
					IGarrison garrison = gameEntity as IGarrison;
					if (garrison is City)
					{
						City city = garrison as City;
						return city.BesiegingEmpire == null || city.BesiegingEmpire == base.Empire;
					}
				}
				break;
			}
			else
			{
				i++;
			}
		}
		return false;
	}

	public bool IsEmpireInfiltrated(global::Empire targetEmpire)
	{
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].GarrisonGuid, out gameEntity) && gameEntity is IGarrison && (gameEntity as IGarrison).Empire == targetEmpire)
			{
				return true;
			}
		}
		return false;
	}

	public bool IsGarrisonInfiltrated(IGarrison target)
	{
		if (this.infiltrationProcesses != null && target != null)
		{
			for (int i = 0; i < this.infiltrationProcesses.Count; i++)
			{
				if (this.infiltrationProcesses[i].GarrisonGuid == target.GUID)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsGarrisonVisible(IGarrison target)
	{
		if (target is City)
		{
			City city = target as City;
			global::Empire empire = base.Empire as global::Empire;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (this.visibilityService.IsWorldPositionVisibleFor(city.Districts[i].WorldPosition, empire))
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsHeroNearGarrison(Unit hero, IGarrison target)
	{
		if (hero.Garrison == null)
		{
			return false;
		}
		Army army = hero.Garrison as Army;
		if (army == null)
		{
			return false;
		}
		if (target is City)
		{
			City city = target as City;
			for (int i = 0; i < city.Districts.Count; i++)
			{
				if (city.Districts[i].WorldPosition == army.WorldPosition)
				{
					return true;
				}
			}
		}
		return false;
	}

	public bool IsStopInfiltrationValid(Unit hero, IGarrison garrison)
	{
		int i = 0;
		while (i < this.infiltrationProcesses.Count)
		{
			if (this.infiltrationProcesses[i].GarrisonGuid == garrison.GUID)
			{
				if (this.infiltrationProcesses[i].HeroGuid == hero.GUID && this.infiltrationProcesses[i].SpyState == InfiltrationProcessus.InfiltrationState.OnGoing)
				{
					return true;
				}
				break;
			}
			else
			{
				i++;
			}
		}
		return false;
	}

	public bool CheckBreakInfiltrationOnMove(Army army, WorldPosition to)
	{
		if (army.Hero == null)
		{
			return false;
		}
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].HeroGuid == army.Hero.GUID)
			{
				District district = this.worldPositionningService.GetDistrict(army.WorldPosition);
				District district2 = this.worldPositionningService.GetDistrict(to);
				return district.City != district2.City;
			}
		}
		return false;
	}

	public void ExecuteAntiSpy(GameEntityGUID infiltratedCityGUID, DepartmentOfIntelligence.AntiSpyResult antiSpyResult, float parameter, bool silent = false)
	{
		SpiedGarrison spiedGarrison = this.spiedGarrisons.Find((SpiedGarrison garrison) => garrison.GUID == infiltratedCityGUID);
		if (spiedGarrison == null)
		{
			Diagnostics.LogError("Failed getting concerned hero.");
			return;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(infiltratedCityGUID, out gameEntity))
		{
			return;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			return;
		}
		Unit hero = spiedGarrison.Hero;
		switch (antiSpyResult)
		{
		case DepartmentOfIntelligence.AntiSpyResult.Wounded:
			this.departmentOfDefense.WoundUnitByAmountInPercent(hero, parameter);
			break;
		case DepartmentOfIntelligence.AntiSpyResult.Injured:
			this.departmentOfEducation.InjureHero(hero, false);
			break;
		case DepartmentOfIntelligence.AntiSpyResult.Captured:
		{
			DepartmentOfEducation.CaptureHero(spiedGarrison.Hero, city.Empire, base.Empire as global::Empire, true, true);
			EventHeroCaptured eventToNotify = new EventHeroCaptured(city.Empire, hero.GUID, city.Empire.Index);
			this.EventService.Notify(eventToNotify);
			break;
		}
		}
		if (silent || antiSpyResult != DepartmentOfIntelligence.AntiSpyResult.Nothing)
		{
			EventAntiSpyResult eventToNotify2 = new EventAntiSpyResult(city.Empire, city, antiSpyResult, spiedGarrison.Empire);
			this.EventService.Notify(eventToNotify2);
		}
	}

	public void ExecuteRoundUp(City city)
	{
		if (city == null)
		{
			Diagnostics.LogError("City is null.");
			return;
		}
		OrderRoundUp order = new OrderRoundUp(base.Empire.Index, city.GUID);
		(base.Empire as global::Empire).PlayerControllers.Server.PostOrder(order);
	}

	public InfiltrationProcessus.InfiltrationState GetGarrisonInfiltrationProcessusState(GameEntityGUID garrisonGuidToSpy)
	{
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].GarrisonGuid == garrisonGuidToSpy)
			{
				return this.infiltrationProcesses[i].SpyState;
			}
		}
		return InfiltrationProcessus.InfiltrationState.None;
	}

	public InfiltrationProcessus.InfiltrationState GetHeroInfiltrationProcessusState(GameEntityGUID heroGuidToSpy)
	{
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].HeroGuid == heroGuidToSpy)
			{
				return this.infiltrationProcesses[i].SpyState;
			}
		}
		return InfiltrationProcessus.InfiltrationState.None;
	}

	public bool TryGetSpyOnGarrison(IGarrison garrison, out Unit hero, out InfiltrationProcessus.InfiltrationState state)
	{
		return this.TryGetSpyOnGarrison(garrison.GUID, out hero, out state);
	}

	public bool TryGetSpyOnGarrison(GameEntityGUID garrisonGuid, out Unit hero, out InfiltrationProcessus.InfiltrationState state)
	{
		hero = null;
		state = InfiltrationProcessus.InfiltrationState.None;
		if (!garrisonGuid.IsValid)
		{
			return false;
		}
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].GarrisonGuid == garrisonGuid)
			{
				state = this.infiltrationProcesses[i].SpyState;
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].HeroGuid, out gameEntity))
				{
					hero = (gameEntity as Unit);
				}
				return true;
			}
		}
		return false;
	}

	public bool TryGetGarrisonForSpy(Unit hero, out IGarrison garrison, out InfiltrationProcessus.InfiltrationState state)
	{
		return this.TryGetGarrisonForSpy(hero.GUID, out garrison, out state);
	}

	public bool TryGetGarrisonForSpy(GameEntityGUID heroGuid, out IGarrison garrison, out InfiltrationProcessus.InfiltrationState state)
	{
		garrison = null;
		state = InfiltrationProcessus.InfiltrationState.None;
		if (!heroGuid.IsValid)
		{
			return false;
		}
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].HeroGuid == heroGuid)
			{
				state = this.infiltrationProcesses[i].SpyState;
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].GarrisonGuid, out gameEntity))
				{
					garrison = (gameEntity as IGarrison);
				}
				return true;
			}
		}
		return false;
	}

	public bool TryGetGarrisonForSpy(GameEntityGUID heroGuid, out IGarrison garrison)
	{
		garrison = null;
		if (!heroGuid.IsValid)
		{
			return false;
		}
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].HeroGuid == heroGuid)
			{
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].GarrisonGuid, out gameEntity))
				{
					garrison = (gameEntity as IGarrison);
				}
				return true;
			}
		}
		return false;
	}

	public void ComputeCurrentInfiltrationLevel(GameEntityGUID garrisonGuid, out int currentInfiltrationLevel, out int maximumInfiltrationLevel)
	{
		currentInfiltrationLevel = 0;
		maximumInfiltrationLevel = 0;
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			if (this.spiedGarrisons[i].GUID == garrisonGuid)
			{
				currentInfiltrationLevel = (int)this.spiedGarrisons[i].GetPropertyValue(SimulationProperties.InfiltrateLevel);
			}
		}
	}

	public bool TryGetInfiltrationStockValue(GameEntityGUID garrisonGuid, out float stock)
	{
		stock = 0f;
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			if (this.spiedGarrisons[i].GUID == garrisonGuid)
			{
				return this.departmentOfTheTreasury.TryGetResourceStockValue(this.spiedGarrisons[i], DepartmentOfTheTreasury.Resources.InfiltrationPoint, out stock, false);
			}
		}
		return false;
	}

	public bool TryTransferInfiltrationStock(GameEntityGUID garrisonGuid, float stock)
	{
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			if (this.spiedGarrisons[i].GUID == garrisonGuid)
			{
				return this.departmentOfTheTreasury.TryTransferResources(this.spiedGarrisons[i], DepartmentOfTheTreasury.Resources.InfiltrationPoint, stock);
			}
		}
		return false;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		int attribute = reader.GetAttribute<int>("Count");
		if (reader.IsStartElement("SpyedGarrisons"))
		{
			reader.ReadStartElement("SpyedGarrisons");
			for (int i = 0; i < attribute; i++)
			{
				GameEntityGUID guid = reader.GetAttribute<ulong>("GarrisonGuid");
				SpiedGarrison spiedGarrison = new SpiedGarrison(guid);
				spiedGarrison.Empire = (base.Empire as global::Empire);
				base.Empire.AddChild(spiedGarrison);
				spiedGarrison.ReadXml(reader);
				reader.ReadEndElement();
				SimulationDescriptor descriptor = null;
				if (this.simulationDescriptorDatabase.TryGetValue("SpiedGarrison", out descriptor))
				{
					spiedGarrison.AddDescriptor(descriptor, false);
				}
				else
				{
					Diagnostics.LogError("Unable to retrieve the 'SpiedGarrison' simulation descriptor from the database.");
				}
				spiedGarrison.RemoveDescriptorByName("SpyedGarrison");
				this.spiedGarrisons.Add(spiedGarrison);
			}
			reader.ReadEndElement("SpyedGarrisons");
		}
		else
		{
			reader.ReadStartElement("SpiedGarrisons");
			for (int j = 0; j < attribute; j++)
			{
				GameEntityGUID guid2 = reader.GetAttribute<ulong>("GarrisonGuid");
				int attribute2 = reader.GetAttribute<int>("ActionInfiltrationCount");
				SpiedGarrison spiedGarrison2 = new SpiedGarrison(guid2);
				spiedGarrison2.ActionInfiltrationCount = attribute2;
				spiedGarrison2.Empire = (base.Empire as global::Empire);
				base.Empire.AddChild(spiedGarrison2);
				spiedGarrison2.ReadXml(reader);
				reader.ReadEndElement();
				this.spiedGarrisons.Add(spiedGarrison2);
			}
			reader.ReadEndElement("SpiedGarrisons");
		}
		int attribute3 = reader.GetAttribute<int>("Count");
		reader.ReadStartElement("Infiltrations");
		this.infiltrationProcesses.Clear();
		for (int k = 0; k < attribute3; k++)
		{
			InfiltrationProcessus item = new InfiltrationProcessus();
			reader.ReadElementSerializable<InfiltrationProcessus>("InfiltrationProcess", ref item);
			this.infiltrationProcesses.Add(item);
		}
		reader.ReadEndElement("Infiltrations");
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("SpiedGarrisons");
		writer.WriteAttributeString<int>("Count", this.SpiedGarrisons.Count);
		for (int i = 0; i < this.SpiedGarrisons.Count; i++)
		{
			writer.WriteStartElement("SpiedGarrison");
			writer.WriteAttributeString<ulong>("GarrisonGuid", this.spiedGarrisons[i].GUID);
			writer.WriteAttributeString<int>("ActionInfiltrationCount", this.spiedGarrisons[i].ActionInfiltrationCount);
			this.spiedGarrisons[i].WriteXml(writer);
			writer.WriteEndElement();
		}
		writer.WriteEndElement();
		writer.WriteStartElement("Infiltrations");
		writer.WriteAttributeString<int>("Count", this.infiltrationProcesses.Count);
		for (int j = 0; j < this.infiltrationProcesses.Count; j++)
		{
			InfiltrationProcessus infiltrationProcessus = this.infiltrationProcesses[j];
			writer.WriteElementSerializable<InfiltrationProcessus>("InfiltrationProcess", ref infiltrationProcessus);
		}
		writer.WriteEndElement();
	}

	private bool AntiSpyCheckPreprocessor(OrderAntiSpyCheck order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.InfiltratedCityGUID.IsValid)
		{
			Diagnostics.LogError("InfiltratedCityGUID can't be invalid.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		if (!this.spiedGarrisons.Exists((SpiedGarrison match) => match.GUID == order.InfiltratedCityGUID))
		{
			return false;
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult((City)gameEntity, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator AntiSpyCheckProcessor(OrderAntiSpyCheck order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		this.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool DamageFortificationByInfiltrationPreprocessor(OrderDamageFortificationByInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity == null)
		{
			return false;
		}
		InfiltrationAction infiltrationAction = null;
		if (!this.infiltrationActionDatabase.TryGetValue(order.InfiltrationActionName, out infiltrationAction))
		{
			return false;
		}
		InfiltrationActionOnCity_DamageFortification infiltrationActionOnCity_DamageFortification = infiltrationAction as InfiltrationActionOnCity_DamageFortification;
		if (infiltrationActionOnCity_DamageFortification == null)
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		float damageToDeal = infiltrationActionOnCity_DamageFortification.InstantDamagePercent * infiltratedCity.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
		order.DamageToDeal = damageToDeal;
		if (infiltrationActionOnCity_DamageFortification.BoosterReferences != null && infiltrationActionOnCity_DamageFortification.BoosterReferences.Length > 0)
		{
			BoosterDefinition boosterDefinition = null;
			IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
			DepartmentOfPlanificationAndDevelopment agency = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			order.BoosterDeclarations = new OrderBuyoutAndActivateBoosterByInfiltration.BoosterDeclaration[infiltrationActionOnCity_DamageFortification.BoosterReferences.Length];
			int duration = infiltrationActionOnCity_DamageFortification.Duration;
			for (int i = 0; i < infiltrationActionOnCity_DamageFortification.BoosterReferences.Length; i++)
			{
				if (!database.TryGetValue(infiltrationActionOnCity_DamageFortification.BoosterReferences[i], out boosterDefinition))
				{
					return false;
				}
				int num = 0;
				if (duration <= 0)
				{
					num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, infiltratedCity, boosterDefinition);
				}
				if (num <= 0)
				{
					num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, duration);
				}
				Booster booster = agency.Boosters.Values.FirstOrDefault((Booster match) => match.BoosterDefinition.Name == boosterDefinition.Name && match.Context == infiltratedCity && match.InstigatorEmpireIndex == this.Empire.Index);
				GameEntityGUID gameEntityGUID;
				if (booster == null)
				{
					gameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
				}
				else
				{
					gameEntityGUID = booster.GUID;
				}
				order.BoosterDeclarations[i] = new OrderBuyoutAndActivateBoosterByInfiltration.BoosterDeclaration
				{
					BoosterDefinitionName = boosterDefinition.Name,
					Duration = num,
					GameEntityGUID = gameEntityGUID
				};
			}
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(infiltratedCity, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator DamageFortificationByInfiltrationProcessor(OrderDamageFortificationByInfiltration order)
	{
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			yield break;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity != null)
		{
			float currentDefensePoint = infiltratedCity.GetPropertyValue(SimulationProperties.CityDefensePoint);
			currentDefensePoint = Mathf.Max(0f, currentDefensePoint - order.DamageToDeal);
			infiltratedCity.SetPropertyBaseValue(SimulationProperties.CityDefensePoint, currentDefensePoint);
			if (order.BoosterDeclarations != null && order.BoosterDeclarations.Length > 0)
			{
				IDatabase<BoosterDefinition> boosterDefinitions = Databases.GetDatabase<BoosterDefinition>(false);
				BoosterDefinition boosterDefinition = null;
				DepartmentOfPlanificationAndDevelopment infiltratedPlanificationAndDevelopment = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
				for (int index = 0; index < order.BoosterDeclarations.Length; index++)
				{
					if (!string.IsNullOrEmpty(order.BoosterDeclarations[index].BoosterDefinitionName) && boosterDefinitions.TryGetValue(order.BoosterDeclarations[index].BoosterDefinitionName, out boosterDefinition))
					{
						infiltratedPlanificationAndDevelopment.ActivateBooster(boosterDefinition, order.BoosterDeclarations[index].GameEntityGUID, order.BoosterDeclarations[index].Duration, infiltratedCity, infiltratedCity.GUID, base.Empire.Index);
					}
				}
			}
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		this.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool DamageGovernorByInfiltrationPreprocessor(OrderDamageGovernorByInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity == null)
		{
			return false;
		}
		if (infiltratedCity.Hero == null)
		{
			return false;
		}
		InfiltrationAction infiltrationAction = null;
		if (!this.infiltrationActionDatabase.TryGetValue(order.InfiltrationActionName, out infiltrationAction))
		{
			return false;
		}
		InfiltrationActionOnCity_PoisonGovernor infiltrationActionOnCity_PoisonGovernor = infiltrationAction as InfiltrationActionOnCity_PoisonGovernor;
		if (infiltrationActionOnCity_PoisonGovernor == null)
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		float damageToDeal = infiltrationActionOnCity_PoisonGovernor.InstantDamagePercent * infiltratedCity.Hero.GetPropertyValue(SimulationProperties.MaximumHealth);
		order.DamageToDeal = damageToDeal;
		if (infiltrationActionOnCity_PoisonGovernor.BoosterReferences != null && infiltrationActionOnCity_PoisonGovernor.BoosterReferences.Length > 0)
		{
			BoosterDefinition boosterDefinition = null;
			IDatabase<BoosterDefinition> database = Databases.GetDatabase<BoosterDefinition>(false);
			DepartmentOfPlanificationAndDevelopment agency = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			order.BoosterDeclarations = new OrderBuyoutAndActivateBoosterByInfiltration.BoosterDeclaration[infiltrationActionOnCity_PoisonGovernor.BoosterReferences.Length];
			int duration = infiltrationActionOnCity_PoisonGovernor.Duration;
			for (int i = 0; i < infiltrationActionOnCity_PoisonGovernor.BoosterReferences.Length; i++)
			{
				if (!database.TryGetValue(infiltrationActionOnCity_PoisonGovernor.BoosterReferences[i], out boosterDefinition))
				{
					return false;
				}
				int num = 0;
				if (duration <= 0)
				{
					num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, infiltratedCity, boosterDefinition);
				}
				if (num <= 0)
				{
					num = DepartmentOfPlanificationAndDevelopment.GetBoosterDurationWithBonus(base.Empire, duration);
				}
				Booster booster = agency.Boosters.Values.FirstOrDefault((Booster match) => match.BoosterDefinition.Name == boosterDefinition.Name && match.Context == infiltratedCity && match.InstigatorEmpireIndex == this.Empire.Index);
				GameEntityGUID gameEntityGUID;
				if (booster == null)
				{
					gameEntityGUID = this.gameEntityRepositoryService.GenerateGUID();
				}
				else
				{
					gameEntityGUID = booster.GUID;
				}
				order.BoosterDeclarations[i] = new OrderBuyoutAndActivateBoosterByInfiltration.BoosterDeclaration
				{
					BoosterDefinitionName = boosterDefinition.Name,
					Duration = num,
					GameEntityGUID = gameEntityGUID
				};
			}
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(infiltratedCity, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator DamageGovernorByInfiltrationProcessor(OrderDamageGovernorByInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			yield break;
		}
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity != null && infiltratedCity.Hero != null)
		{
			float currentHealth = infiltratedCity.Hero.GetPropertyValue(SimulationProperties.Health);
			currentHealth -= order.DamageToDeal;
			infiltratedCity.Hero.SetPropertyBaseValue(SimulationProperties.Health, currentHealth);
			DepartmentOfDefense defense = infiltratedCity.Empire.GetAgency<DepartmentOfDefense>();
			defense.UpdateLifeAfterEncounter(infiltratedCity);
			defense.CleanGarrisonAfterEncounter(infiltratedCity);
			if (order.BoosterDeclarations != null && order.BoosterDeclarations.Length > 0)
			{
				IDatabase<BoosterDefinition> boosterDefinitions = Databases.GetDatabase<BoosterDefinition>(false);
				BoosterDefinition boosterDefinition = null;
				DepartmentOfPlanificationAndDevelopment infiltratedPlanificationAndDevelopment = infiltratedCity.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
				for (int index = 0; index < order.BoosterDeclarations.Length; index++)
				{
					if (!string.IsNullOrEmpty(order.BoosterDeclarations[index].BoosterDefinitionName) && boosterDefinitions.TryGetValue(order.BoosterDeclarations[index].BoosterDefinitionName, out boosterDefinition))
					{
						infiltratedPlanificationAndDevelopment.ActivateBooster(boosterDefinition, order.BoosterDeclarations[index].GameEntityGUID, order.BoosterDeclarations[index].Duration, infiltratedCity.Hero, infiltratedCity.Hero.GUID, base.Empire.Index);
					}
				}
			}
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		this.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool RevealInfiltratedSpiesByInfiltrationPreprocessor(OrderRevealInfiltratedSpiesByInfiltration order)
	{
		InfiltrationAction infiltrationAction = null;
		if (!this.infiltrationActionDatabase.TryGetValue(order.InfiltrationActionName, out infiltrationAction))
		{
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		City city = gameEntity as City;
		if (city != null)
		{
			bool? flag = new bool?(true);
			InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
			if (!flag.Value)
			{
				return false;
			}
			DepartmentOfIntelligence agency = city.Empire.GetAgency<DepartmentOfIntelligence>();
			if (agency != null)
			{
				List<Unit> list = new List<Unit>();
				if (agency.SpiedGarrisons != null && agency.SpiedGarrisons.Count > 0)
				{
					for (int i = 0; i < agency.SpiedGarrisons.Count; i++)
					{
						if (this.gameEntityRepositoryService.TryGetValue(agency.SpiedGarrisons[i].GUID, out gameEntity))
						{
							City city2 = gameEntity as City;
							if (city2 != null && city2.Empire.Index == base.Empire.Index)
							{
								Unit item;
								InfiltrationProcessus.InfiltrationState infiltrationState;
								if (agency.TryGetSpyOnGarrison(agency.SpiedGarrisons[i], out item, out infiltrationState) && infiltrationState == InfiltrationProcessus.InfiltrationState.Infiltrated)
								{
									list.Add(item);
								}
							}
						}
					}
				}
				int count = Math.Min(list.Count, Math.Max(1, order.NumberOfSpiesToReveal));
				if (order.NumberOfSpiesToReveal == -1)
				{
					count = list.Count;
				}
				Unit[] source = (from unit in list
				orderby unit.GetPropertyValue(SimulationProperties.InfiltrationCooldown)
				select unit).Reverse<Unit>().Take(count).ToArray<Unit>();
				order.InfiltratedHeroGUIDs = (from unit in source
				select unit.GUID).ToArray<GameEntityGUID>();
			}
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(city, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator RevealInfiltratedSpiesByInfiltrationProcessor(OrderRevealInfiltratedSpiesByInfiltration order)
	{
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		IGameEntity gameEntity = null;
		bool notified = false;
		if (order.InfiltratedHeroGUIDs != null && order.InfiltratedHeroGUIDs.Length > 0 && this.EventService != null)
		{
			notified = true;
			this.EventService.Notify(new EventInfiltratedHeroesRevealed(base.Empire, order.InfiltratedCityGUID, order.InfiltratedHeroGUIDs, order.NumberOfSpiesToReveal));
		}
		if (!notified && this.EventService != null)
		{
			this.EventService.Notify(new EventInfiltratedHeroesRevealed(base.Empire, order.InfiltratedCityGUID, null, order.NumberOfSpiesToReveal));
		}
		if (order.InfiltratedHeroGUIDs != null && order.InfiltratedHeroGUIDs.Length > 0)
		{
			int spiesRevealedCount = (order.NumberOfSpiesToReveal >= 0) ? order.NumberOfSpiesToReveal : order.InfiltratedHeroGUIDs.Length;
			for (int index = 0; index < spiesRevealedCount; index++)
			{
				if (this.gameEntityRepositoryService.TryGetValue(order.InfiltratedHeroGUIDs[index], out gameEntity))
				{
					Unit unitToWound = gameEntity as Unit;
					if (unitToWound != null)
					{
						DepartmentOfDefense unitDepartmentOfDefense = unitToWound.Garrison.Empire.GetAgency<DepartmentOfDefense>();
						if (unitDepartmentOfDefense != null)
						{
							unitDepartmentOfDefense.WoundUnitByAmountInPercent(unitToWound, order.Damage);
						}
					}
				}
			}
		}
		InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
		if (this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			City infiltratedCity = gameEntity as City;
			if (infiltratedCity != null)
			{
				InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
			}
		}
		this.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool RoundUpPreprocessor(OrderRoundUp order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.InfiltratedCityGUID.IsValid)
		{
			Diagnostics.LogError("InfiltratedCityGUID can't be invalid.");
			return false;
		}
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			return false;
		}
		order.HeroesGUIDs = (from garrison in DepartmentOfIntelligence.GetCityInfiltratedSpies(order.InfiltratedCityGUID)
		select garrison.Hero.GUID).ToArray<GameEntityGUID>();
		if (order.HeroesGUIDs == null)
		{
			return false;
		}
		order.AntiSpyResults = new DepartmentOfIntelligence.AntiSpyResult[order.HeroesGUIDs.Length];
		order.AntiSpyParameters = new float[order.HeroesGUIDs.Length];
		City city = gameEntity as City;
		for (int i = 0; i < order.AntiSpyResults.Length; i++)
		{
			float num;
			order.AntiSpyResults[i] = DepartmentOfIntelligence.GetAntiSpyResult(city, out num);
			order.AntiSpyParameters[i] = num;
		}
		return true;
	}

	private IEnumerator RoundUpProcessor(OrderRoundUp order)
	{
		if (order == null)
		{
			throw new ArgumentNullException("order");
		}
		if (!order.InfiltratedCityGUID.IsValid)
		{
			Diagnostics.LogError("InfiltratedCityGUID can't be invalid.");
			yield break;
		}
		IGameEntity gameEntity;
		for (int index = 0; index < order.HeroesGUIDs.Length; index++)
		{
			if (!this.gameEntityRepositoryService.TryGetValue(order.HeroesGUIDs[index], out gameEntity))
			{
				Diagnostics.LogError("Fail getting garrison entity");
				yield break;
			}
			Unit hero = gameEntity as Unit;
			if (hero == null || hero.Garrison == null)
			{
				yield break;
			}
			DepartmentOfIntelligence intelligence = hero.Garrison.Empire.GetAgency<DepartmentOfIntelligence>();
			if (intelligence != null)
			{
				this.EventService.Notify(new EventRoundUpEndTarget(hero.Garrison.Empire, order.InfiltratedCityGUID, hero.GUID, order.AntiSpyResults[index]));
				intelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResults[index], order.AntiSpyParameters[index], true);
			}
		}
		this.EventService.Notify(new EventRoundUpEndInitiator(base.Empire, order.InfiltratedCityGUID, order.HeroesGUIDs, order.AntiSpyResults));
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Fail getting city entity");
			yield break;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			yield break;
		}
		this.departmentOfTheInterior.ToggleCityRoundUp(city);
		yield break;
	}

	private bool StartLeechByInfiltrationPreprocessor(OrderStartLeechByInfiltration order)
	{
		InfiltrationAction infiltrationAction = null;
		if (!this.infiltrationActionDatabase.TryGetValue(order.InfiltrationActionName, out infiltrationAction))
		{
			return false;
		}
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		LeechDefinition[] array = new LeechDefinition[order.LeechDescriptions.Length];
		for (int i = 0; i < order.LeechDescriptions.Length; i++)
		{
			if (!this.leechDefinitionDatabase.TryGetValue(order.LeechDescriptions[i].LeechDefinitionName, out array[i]))
			{
				return false;
			}
			if (order.LeechDescriptions[i].Duration < 0)
			{
				order.LeechDescriptions[i].Duration = array[i].Duration;
			}
		}
		GameEntityGUID[] array2 = new GameEntityGUID[order.LeechDescriptions.Length];
		foreach (Leech leech in this.leechService.GetLeechesOn(city))
		{
			if (leech.LeecherEmpire == base.Empire)
			{
				bool flag2 = false;
				for (int j = 0; j < array.Length; j++)
				{
					if (!(array[j].LeechedPropertyName != leech.LeechDefinition.LeechedPropertyName) && !(array[j].ValidationPath.ToString() != leech.LeechDefinition.ValidationPath.ToString()))
					{
						if (flag2)
						{
							return false;
						}
						if (array[j].LeechPercentage < leech.LeechDefinition.LeechPercentage)
						{
							return false;
						}
						array2[j] = leech.GUID;
						flag2 = true;
					}
				}
			}
		}
		for (int k = 0; k < order.LeechDescriptions.Length; k++)
		{
			if (!array2[k].IsValid)
			{
				array2[k] = this.gameEntityRepositoryService.GenerateGUID();
			}
		}
		order.LeechGuid = array2;
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(city, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		return true;
	}

	private IEnumerator StartLeechByInfiltrationProcessor(OrderStartLeechByInfiltration order)
	{
		InfiltrationAction.TryTransferResources(base.Empire as global::Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.InfiltratedCityGUID, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			yield break;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity != null)
		{
			InfiltrationProcessus infiltrationProcessus = null;
			for (int index = 0; index < this.infiltrationProcesses.Count; index++)
			{
				if (this.infiltrationProcesses[index].GarrisonGuid == infiltratedCity.GUID)
				{
					infiltrationProcessus = this.infiltrationProcesses[index];
					break;
				}
			}
			if (infiltrationProcessus == null)
			{
				yield break;
			}
			for (int index2 = 0; index2 < order.LeechDescriptions.Length; index2++)
			{
				LeechDefinition leechDefinition;
				if (this.leechDefinitionDatabase.TryGetValue(order.LeechDescriptions[index2].LeechDefinitionName, out leechDefinition))
				{
					Leech activeLeech = this.leechService.GetLeech(order.LeechGuid[index2]);
					if (activeLeech == null)
					{
						this.leechService.CreateLeech<City>(order.LeechGuid[index2], leechDefinition, base.Empire as global::Empire, infiltratedCity, order.LeechDescriptions[index2].Duration);
						infiltrationProcessus.LeechGuids.Add(order.LeechGuid[index2]);
					}
					else if (activeLeech.LeechDefinition.LeechPercentage == leechDefinition.LeechPercentage)
					{
						this.leechService.ExtendLeechDuration(activeLeech.GUID, order.LeechDescriptions[index2].Duration);
					}
					else
					{
						this.leechService.ReplaceLeechDefinition(order.LeechGuid[index2], leechDefinition, order.LeechDescriptions[index2].Duration);
						if (!infiltrationProcessus.LeechGuids.Contains(activeLeech.GUID))
						{
							infiltrationProcessus.LeechGuids.Add(order.LeechGuid[index2]);
						}
					}
				}
			}
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(infiltratedCity.Empire, (global::Empire)base.Empire, order);
		}
		this.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		yield break;
	}

	private bool ToggleInfiltrationPreprocessor(OrderToggleInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.HeroGuid, out gameEntity))
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		Unit hero = gameEntity as Unit;
		if (hero == null)
		{
			Diagnostics.LogError("Order preprocessing failed because the target hero is not valid.");
			return false;
		}
		if (DepartmentOfEducation.IsCaptured(hero))
		{
			return false;
		}
		gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.AssignmentGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target garrison game entity is not valid.");
			return false;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison == null)
		{
			return false;
		}
		if (!order.IsStarting)
		{
			return this.IsStopInfiltrationValid(hero, garrison);
		}
		if (hero.Garrison != null && this.battleEncounterRepositoryService != null)
		{
			IEnumerable<BattleEncounter> enumerable = this.battleEncounterRepositoryService;
			if (enumerable != null && enumerable.Any((BattleEncounter encounter) => encounter.IsGarrisonInEncounter(hero.Garrison.GUID)))
			{
				Diagnostics.LogWarning("Order preprocessing failed because the hero is in combat ");
				return false;
			}
		}
		float infiltrationCost = 0f;
		bool result;
		if (order.IgnoreVision)
		{
			result = this.CanInfiltrateIgnoreVision(hero, garrison, order.IsAGroundInfiltration, out infiltrationCost, true);
		}
		else
		{
			result = this.CanInfiltrate(hero, garrison, order.IsAGroundInfiltration, out infiltrationCost, true);
		}
		order.InfiltrationCost = infiltrationCost;
		return result;
	}

	private IEnumerator ToggleInfiltrationProcessor(OrderToggleInfiltration order)
	{
		IGameEntity gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.HeroGuid, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target hero game entity is not valid.");
			yield break;
		}
		Unit hero = gameEntity as Unit;
		if (hero == null)
		{
			yield break;
		}
		gameEntity = null;
		if (!this.gameEntityRepositoryService.TryGetValue(order.AssignmentGUID, out gameEntity))
		{
			Diagnostics.LogError("Order processing failed because the target garrison game entity is not valid.");
			yield break;
		}
		IGarrison garrison = gameEntity as IGarrison;
		if (garrison == null)
		{
			yield break;
		}
		if (order.InfiltrationCost <= 0f || !this.departmentOfTheTreasury.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.InfiltrationCost, -order.InfiltrationCost))
		{
		}
		if (order.IsStarting)
		{
			if (!this.StartInfiltration(garrison, hero, order.IsAGroundInfiltration))
			{
				Diagnostics.LogError("Order processing failed. The garrison is already infiltrated or the hero is not available.");
			}
		}
		else
		{
			this.StopInfiltration(hero, false, true);
		}
		yield break;
	}

	public List<SpiedGarrison> SpiedGarrisons
	{
		get
		{
			return this.spiedGarrisons;
		}
	}

	public List<InfiltrationProcessus> InfiltrationProcesses
	{
		get
		{
			return this.infiltrationProcesses;
		}
	}

	protected IEventService EventService { get; set; }

	private bool EnableDetection { get; set; }

	public void OnInfiltrationSeniorityChange(GameEntityGUID hero, GameEntityGUID garrison)
	{
		if (this.InfiltrationSeniorityChange != null)
		{
			this.InfiltrationSeniorityChange(this, new InfiltrationEventArgs(hero, garrison));
		}
	}

	public void StopInfiltration(Unit unit, bool shouldStopLeech = true, bool notify = true)
	{
		if (unit == null)
		{
			return;
		}
		bool flag = false;
		if (unit.Garrison != null && unit.Garrison is SpiedGarrison)
		{
			unit.Garrison.SetHero(null);
			base.Empire.AddChild(unit);
			base.Empire.Refresh(false);
			flag = true;
		}
		for (int i = this.infiltrationProcesses.Count - 1; i >= 0; i--)
		{
			if (this.infiltrationProcesses[i].HeroGuid == unit.GUID)
			{
				for (int j = this.spiedGarrisons.Count - 1; j >= 0; j--)
				{
					if (this.spiedGarrisons[j].GUID == this.infiltrationProcesses[i].GarrisonGuid)
					{
						this.DestroySpyedGarrison(this.spiedGarrisons[j]);
						break;
					}
				}
				InfiltrationProcessus infiltrationProcessus = this.infiltrationProcesses[i];
				if (shouldStopLeech && infiltrationProcessus.LeechGuids.Count > 0)
				{
					for (int k = this.infiltrationProcesses[i].LeechGuids.Count - 1; k >= 0; k--)
					{
						this.leechService.DestroyLeech(infiltrationProcessus.LeechGuids[k]);
					}
				}
				flag = true;
				this.infiltrationProcesses.RemoveAt(i);
				this.OnInfiltrationStateChange(infiltrationProcessus.HeroGuid, infiltrationProcessus.GarrisonGuid);
				this.OnInfiltrationProcessCollectionChange(infiltrationProcessus, CollectionChangeAction.Remove);
			}
		}
		if (this.EventService != null && notify && flag)
		{
			this.EventService.Notify(new EventHeroExfiltrated(base.Empire, unit));
		}
		this.visibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.simulationDescriptorDatabase = Databases.GetDatabase<SimulationDescriptor>(false);
		this.departmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
		this.departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		Diagnostics.Assert(this.departmentOfEducation != null);
		this.departmentOfDefense = base.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(this.departmentOfDefense != null);
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfTheInterior = base.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfTheInterior.CitiesCollectionChanged += this.DepartmentOfTheInterior_CitiesCollectionChanged;
		IGameService gameService = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
		this.gameEntityRepositoryService.GameEntityRepositoryChange += this.GameEntityRepositoryService_GameEntityRepositoryChange;
		this.EventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.EventService != null);
		this.worldPositionningService = gameService.Game.Services.GetService<IWorldPositionningService>();
		this.battleEncounterRepositoryService = gameService.Game.Services.GetService<IBattleEncounterRepositoryService>();
		this.visibilityService = gameService.Game.Services.GetService<IVisibilityService>();
		this.leechService = gameService.Game.Services.GetService<ILeechService>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.infiltrationActionDatabase = Databases.GetDatabase<InfiltrationAction>(false);
		this.leechDefinitionDatabase = Databases.GetDatabase<LeechDefinition>(false);
		base.Empire.RegisterPass("GameClientState_Turn_End", "UpdateInfiltration", new Agency.Action(this.GameClientState_Turn_End_UpdateInfiltration), new string[]
		{
			"SpyUnitExperiencePerTurnGain"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "UpdateSpyGarrison", new Agency.Action(this.GameClientState_Turn_End_UpdateSpyGarrisons), new string[]
		{
			"CollectResources",
			"UpdateInfiltration"
		});
		base.Empire.RegisterPass("GameClientState_Turn_End", "SpyingUnitHealthPerTurnGain", new Agency.Action(this.GameClientState_Turn_End_SpyingUnitHealthPerTurnGain), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_End", "SpyUnitExperiencePerTurnGain", new Agency.Action(this.GameClientState_Turn_End_UnitExperiencePerTurnGain), new string[0]);
		if (DepartmentOfIntelligence.antiSpyResults == null)
		{
			DepartmentOfIntelligence.antiSpyResults = (DepartmentOfIntelligence.AntiSpyResult[])Enum.GetValues(typeof(DepartmentOfIntelligence.AntiSpyResult));
		}
		this.EnableDetection = false;
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		yield break;
	}

	protected override IEnumerator OnLoadGame(Amplitude.Unity.Game.Game game)
	{
		yield return base.OnLoadGame(game);
		for (int index = this.spiedGarrisons.Count - 1; index >= 0; index--)
		{
			this.gameEntityRepositoryService.Register(this.spiedGarrisons[index].Hero);
			InfiltrationProcessus infiltrationProcessus = this.infiltrationProcesses.FirstOrDefault((InfiltrationProcessus infiltrationProcess) => infiltrationProcess.GarrisonGuid == this.spiedGarrisons[index].GUID);
			if (infiltrationProcessus != null && infiltrationProcessus.SpyState == InfiltrationProcessus.InfiltrationState.Infiltrated)
			{
				City infiltratedCity = null;
				for (int otherEmpireIndex = 0; otherEmpireIndex < ((global::Game)game).Empires.Length; otherEmpireIndex++)
				{
					MajorEmpire otherEmpire = ((global::Game)game).Empires[otherEmpireIndex] as MajorEmpire;
					if (otherEmpire == null)
					{
						break;
					}
					DepartmentOfTheInterior departmentOfTheInterior = otherEmpire.GetAgency<DepartmentOfTheInterior>();
					if (departmentOfTheInterior != null)
					{
						infiltratedCity = departmentOfTheInterior.Cities.FirstOrDefault((City city) => city.GUID == this.spiedGarrisons[index].GUID);
						if (infiltratedCity != null)
						{
							infiltratedCity.EmpireInfiltrationBits |= 1 << base.Empire.Index;
							break;
						}
					}
				}
				if (infiltratedCity == null || infiltratedCity.Empire == base.Empire)
				{
					this.StopInfiltration(this.spiedGarrisons[index].Hero, true, false);
				}
			}
		}
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null && downloadableContentService.IsShared(DownloadableContent11.ReadOnlyName))
		{
			this.EnableDetection = true;
			DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			if (departmentOfPlanificationAndDevelopment != null)
			{
				departmentOfPlanificationAndDevelopment.BoosterCollectionChange += this.DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange;
				foreach (Booster booster in departmentOfPlanificationAndDevelopment.Boosters.Values)
				{
					this.DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange(this, new BoosterCollectionChangeEventArgs(BoosterCollectionChangeAction.Add, booster));
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		for (int i = 0; i < this.spiedGarrisons.Count; i++)
		{
			base.Empire.RemoveChild(this.spiedGarrisons[i]);
			this.spiedGarrisons[i].Dispose();
		}
		this.spiedGarrisons.Clear();
		this.infiltrationProcesses.Clear();
		if (this.EnableDetection)
		{
			DepartmentOfPlanificationAndDevelopment agency = base.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
			if (agency != null)
			{
				agency.BoosterCollectionChange -= this.DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange;
			}
		}
		this.departmentOfEducation = null;
		this.departmentOfDefense = null;
		if (this.departmentOfTheTreasury != null)
		{
			this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			this.departmentOfTheTreasury = null;
		}
		if (this.departmentOfTheInterior != null)
		{
			this.departmentOfTheInterior.CitiesCollectionChanged -= this.DepartmentOfTheInterior_CitiesCollectionChanged;
			this.departmentOfTheInterior = null;
		}
		this.worldPositionningService = null;
		if (this.gameEntityRepositoryService != null)
		{
			this.gameEntityRepositoryService.GameEntityRepositoryChange -= this.GameEntityRepositoryService_GameEntityRepositoryChange;
			this.gameEntityRepositoryService = null;
		}
		this.simulationDescriptorDatabase = null;
	}

	private bool ComputeSpiedGarrisonRegenModifier(SpiedGarrison spiedGarrison, out float spiedGarrisonRegenModifier, out int pacifiedVillageCount)
	{
		spiedGarrisonRegenModifier = 0f;
		pacifiedVillageCount = 0;
		IGameEntity gameEntity;
		if (!this.gameEntityRepositoryService.TryGetValue(spiedGarrison.GUID, out gameEntity))
		{
			return false;
		}
		if (gameEntity == null)
		{
			return false;
		}
		City city = gameEntity as City;
		if (city == null)
		{
			return false;
		}
		StaticString propertyName;
		if (!this.departmentOfDefense.ComputeEmpireRegenModifierPropertyNameForRegion(city.Region, out propertyName))
		{
			return false;
		}
		spiedGarrisonRegenModifier = base.Empire.GetPropertyValue(propertyName);
		spiedGarrisonRegenModifier += spiedGarrison.GetPropertyValue(propertyName);
		if (city.BesiegingEmpire == null && city.Region != null)
		{
			pacifiedVillageCount = Mathf.FloorToInt(city.GetPropertyValue(SimulationProperties.NumberOfRebuildPacifiedVillage));
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.DiplomaticRelations[city.Empire.Index];
			if (diplomaticRelation != null && diplomaticRelation.State != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
			{
				spiedGarrisonRegenModifier += city.GetPropertyValue(SimulationProperties.AlliedUnitRegenModifier);
			}
		}
		return true;
	}

	private void DepartmentOfPlanificationAndDevelopment_BoosterCollectionChange(object sender, BoosterCollectionChangeEventArgs e)
	{
		if (e.Booster != null && e.Booster.BoosterDefinition != null && e.Booster.BoosterDefinition.Name.ToString().StartsWith("BoosterStealVisionOverArmiesByInfiltration"))
		{
			int num = -1;
			if (int.TryParse(e.Booster.BoosterDefinition.Name.ToString().Substring("BoosterStealVisionOverArmiesByInfiltration".Length), out num))
			{
				IGameService service = Services.GetService<IGameService>();
				if (service != null && service.Game != null && service.Game is global::Game)
				{
					global::Game game = service.Game as global::Game;
					if (game.Empires != null && num >= 0 && num < game.Empires.Length)
					{
						MajorEmpire majorEmpire = game.Empires[num] as MajorEmpire;
						int num2 = 1 << base.Empire.Index;
						BoosterCollectionChangeAction action = e.Action;
						if (action != BoosterCollectionChangeAction.Add)
						{
							if (action == BoosterCollectionChangeAction.Remove)
							{
								majorEmpire.ArmiesInfiltrationBits &= ~num2;
							}
						}
						else
						{
							majorEmpire.ArmiesInfiltrationBits |= num2;
						}
						this.visibilityService.NotifyVisibilityHasChanged((global::Empire)base.Empire);
						this.visibilityService.NotifyVisibilityHasChanged(majorEmpire);
					}
				}
			}
		}
	}

	private void DepartmentOfTheInterior_CitiesCollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		if (e.Action == CollectionChangeAction.Add)
		{
			City city = e.Element as City;
			if (city == null)
			{
				return;
			}
			this.CheckStopInfiltrationAgainstGarrisonChange(city.GUID);
			List<Leech> list = new List<Leech>();
			foreach (Leech leech in this.leechService.GetLeechesOn(city))
			{
				if (leech.LeecherEmpire == base.Empire)
				{
					list.Add(leech);
				}
			}
			for (int i = 0; i < list.Count; i++)
			{
				this.leechService.DestroyLeech(list[i].GUID);
			}
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.ResourceDefinition.Name == DepartmentOfTheTreasury.Resources.InfiltrationPoint)
		{
			for (int i = 0; i < this.spiedGarrisons.Count; i++)
			{
				if (this.spiedGarrisons[i].SimulationObject == e.Location)
				{
					this.UpdateInfiltrationLevel(this.spiedGarrisons[i]);
					break;
				}
			}
		}
	}

	private void GameEntityRepositoryService_GameEntityRepositoryChange(object sender, GameEntityRepositoryChangeEventArgs e)
	{
		if (e.Action == GameEntityRepositoryChangeAction.Remove)
		{
			this.CheckStopInfiltrationAgainstGarrisonChange(e.GameEntityGuid);
		}
	}

	private IEnumerator GameClientState_Turn_End_UpdateInfiltration(string context, string name)
	{
		for (int index = this.infiltrationProcesses.Count - 1; index >= 0; index--)
		{
			InfiltrationProcessus infiltrationProcessus = this.infiltrationProcesses[index];
			for (int leechIndex = infiltrationProcessus.LeechGuids.Count - 1; leechIndex >= 0; leechIndex--)
			{
				if (infiltrationProcessus.LeechGuids[leechIndex].IsValid && this.leechService.GetLeech(infiltrationProcessus.LeechGuids[leechIndex]) == null)
				{
					infiltrationProcessus.LeechGuids.RemoveAt(leechIndex);
				}
			}
			if (infiltrationProcessus.SpyState == InfiltrationProcessus.InfiltrationState.OnGoing)
			{
				if (!this.UpdateInfiltrationCooldown(infiltrationProcessus, true))
				{
					this.spiedGarrisons.RemoveAll((SpiedGarrison match) => match.Hero == null || match.Hero.GUID == infiltrationProcessus.HeroGuid);
					InfiltrationProcessus process = infiltrationProcessus;
					this.infiltrationProcesses.RemoveAt(index);
					this.OnInfiltrationProcessCollectionChange(process, CollectionChangeAction.Remove);
				}
			}
			else if (infiltrationProcessus.SpyState == InfiltrationProcessus.InfiltrationState.Infiltrated)
			{
				Unit hero = null;
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(infiltrationProcessus.HeroGuid, out gameEntity))
				{
					hero = (gameEntity as Unit);
				}
				City city = null;
				if (this.gameEntityRepositoryService.TryGetValue(infiltrationProcessus.GarrisonGuid, out gameEntity))
				{
					city = (gameEntity as City);
				}
				if (hero == null || city == null)
				{
					this.spiedGarrisons.RemoveAll((SpiedGarrison match) => match.Hero == null || match.Hero.GUID == infiltrationProcessus.HeroGuid);
					InfiltrationProcessus process2 = infiltrationProcessus;
					this.infiltrationProcesses.RemoveAt(index);
					this.OnInfiltrationProcessCollectionChange(process2, CollectionChangeAction.Remove);
				}
				else if (!this.spiedGarrisons.Exists((SpiedGarrison match) => match.Hero != null && match.Hero.GUID == infiltrationProcessus.HeroGuid))
				{
					this.CreateSpyedGarrison(city, hero);
				}
			}
			else
			{
				this.infiltrationProcesses.RemoveAt(index);
			}
		}
		yield break;
	}

	private bool UpdateInfiltrationCooldown(InfiltrationProcessus infiltrationProcessus, bool allowIncrement)
	{
		Unit unit = null;
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(infiltrationProcessus.HeroGuid, out gameEntity))
		{
			unit = (gameEntity as Unit);
		}
		if (unit == null)
		{
			return false;
		}
		float num = unit.GetPropertyValue(SimulationProperties.InfiltrationCooldown);
		float propertyValue = unit.GetPropertyValue(SimulationProperties.MaximumInfiltrationCooldown);
		City city = null;
		if (this.gameEntityRepositoryService.TryGetValue(infiltrationProcessus.GarrisonGuid, out gameEntity))
		{
			city = (gameEntity as City);
		}
		if (city == null || city.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
		{
			return false;
		}
		if (allowIncrement && (city.BesiegingEmpire == null || city.BesiegingEmpire == base.Empire))
		{
			num += 1f;
		}
		if (num > propertyValue)
		{
			num = propertyValue;
		}
		unit.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, num);
		if (num >= propertyValue)
		{
			infiltrationProcessus.SpyState = InfiltrationProcessus.InfiltrationState.Infiltrated;
			this.CreateSpyedGarrison(city, unit);
			this.OnInfiltrationStateChange(infiltrationProcessus.HeroGuid, infiltrationProcessus.GarrisonGuid);
		}
		return true;
	}

	private IEnumerator GameClientState_Turn_End_SpyingUnitHealthPerTurnGain(string context, string name)
	{
		for (int index = 0; index < this.spiedGarrisons.Count; index++)
		{
			float regenModifier;
			int pacifiedVillageCount;
			if (this.ComputeSpiedGarrisonRegenModifier(this.spiedGarrisons[index], out regenModifier, out pacifiedVillageCount))
			{
				DepartmentOfDefense.RegenUnit(this.spiedGarrisons[index].Hero, regenModifier, pacifiedVillageCount);
				this.spiedGarrisons[index].Hero.Refresh(true);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UpdateSpyGarrisons(string context, string name)
	{
		for (int index = this.spiedGarrisons.Count - 1; index >= 0; index--)
		{
			if (!this.spiedGarrisons[index].GUID.IsValid || this.spiedGarrisons[index].Hero == null)
			{
				this.spiedGarrisons.RemoveAt(index);
			}
			else
			{
				this.UpdateInfiltrationLevel(this.spiedGarrisons[index]);
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_UnitExperiencePerTurnGain(string context, string name)
	{
		City city = null;
		for (int index = 0; index < this.spiedGarrisons.Count; index++)
		{
			city = null;
			float population = 0f;
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[index].GarrisonGuid, out gameEntity))
			{
				city = (gameEntity as City);
			}
			if (city != null)
			{
				population = city.GetPropertyValue(SimulationProperties.Population);
			}
			this.spiedGarrisons[index].SetPropertyBaseValue(SimulationProperties.InfiltratedTargetPopulation, population);
			this.spiedGarrisons[index].Refresh(false);
			if (this.spiedGarrisons[index].Hero != null)
			{
				float experienceGain = this.spiedGarrisons[index].Hero.GetPropertyValue(SimulationProperties.UnitExperienceGainPerTurn);
				this.spiedGarrisons[index].Hero.GainXp(experienceGain, false, true);
			}
		}
		yield break;
	}

	private void UpdateInfiltrationLevel(SpiedGarrison spiedGarrison)
	{
		float num = spiedGarrison.GetPropertyValue(SimulationProperties.InfiltrateLevel);
		float num2 = num;
		float num3 = 0f;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(spiedGarrison, DepartmentOfTheTreasury.Resources.InfiltrationPoint, out num3, false))
		{
			Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
			{
				DepartmentOfTheTreasury.Resources.InfiltrationPoint,
				spiedGarrison.GUID
			});
			return;
		}
		float propertyValue = spiedGarrison.GetPropertyValue(SimulationProperties.MaximumInfiltrateLevel);
		float a = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, propertyValue) + 1f;
		num3 = Mathf.Min(a, num3);
		spiedGarrison.SetPropertyBaseValue("InfiltrationPointStock", num3);
		float num4 = 0f;
		if (num > 1f)
		{
			num4 = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, num);
		}
		float num5 = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, num + 1f);
		float num6 = 0f;
		if (num3 < num4)
		{
			num6 = -1f;
		}
		else if (num3 >= num5)
		{
			num6 = 1f;
		}
		while (num6 != 0f)
		{
			num += num6;
			if (num <= 0f)
			{
				num = 0f;
				break;
			}
			num4 = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, num);
			num5 = DepartmentOfIntelligence.ComputeInfiltrateLevelInInfiltratePointStock(spiedGarrison, num + 1f);
			num6 = 0f;
			if (num3 < num4)
			{
				num6 = -1f;
			}
			else if (num3 >= num5)
			{
				num6 = 1f;
			}
		}
		spiedGarrison.SetPropertyBaseValue(SimulationProperties.InfiltrateLevel, num);
		spiedGarrison.Refresh(false);
		if (num > num2 && num != 1f)
		{
			Unit hero = null;
			InfiltrationProcessus.InfiltrationState infiltrationState = InfiltrationProcessus.InfiltrationState.Infiltrated;
			if (this.TryGetSpyOnGarrison(spiedGarrison, out hero, out infiltrationState))
			{
				this.EventService.Notify(new EventInfiltrationLevelProgress(base.Empire, hero));
			}
		}
		this.OnInfiltrationLevelChange(spiedGarrison.Hero.GUID, spiedGarrison.GUID);
	}

	private void DestroySpyedGarrison(SpiedGarrison spiedGarrison)
	{
		if (this.spiedGarrisons.Remove(spiedGarrison))
		{
			base.Empire.RemoveChild(spiedGarrison);
			spiedGarrison.Dispose();
		}
	}

	public void CheckStopInfiltrationAgainstGarrisonChange(GameEntityGUID garrisonGuid)
	{
		for (int i = 0; i < this.infiltrationProcesses.Count; i++)
		{
			if (this.infiltrationProcesses[i].GarrisonGuid == garrisonGuid)
			{
				IGameEntity gameEntity;
				if (this.gameEntityRepositoryService.TryGetValue(this.infiltrationProcesses[i].HeroGuid, out gameEntity))
				{
					this.StopInfiltration(gameEntity as Unit, true, true);
				}
				break;
			}
		}
	}

	private bool StartInfiltration(IGarrison garrison, Unit hero, bool isGroundInfiltration)
	{
		if (this.InfiltrationProcesses.Exists((InfiltrationProcessus match) => match.GarrisonGuid == garrison.GUID))
		{
			return false;
		}
		this.StopInfiltration(hero, false, false);
		if (!isGroundInfiltration && hero.Garrison != null)
		{
			this.departmentOfEducation.UnassignHero(hero);
		}
		InfiltrationProcessus infiltrationProcessus = new InfiltrationProcessus();
		infiltrationProcessus.HeroGuid = hero.GUID;
		infiltrationProcessus.GarrisonGuid = garrison.GUID;
		infiltrationProcessus.SpyState = InfiltrationProcessus.InfiltrationState.OnGoing;
		infiltrationProcessus.IsAGroundInfiltration = isGroundInfiltration;
		infiltrationProcessus.TurnAtStart = this.endTurnService.Turn;
		hero.SetPropertyBaseValue(SimulationProperties.InfiltrationCooldown, 0f);
		float propertyValue;
		if (isGroundInfiltration)
		{
			propertyValue = hero.GetPropertyValue(SimulationProperties.MaximumInfiltrationCooldownFromGround);
		}
		else
		{
			propertyValue = hero.GetPropertyValue(SimulationProperties.MaximumInfiltrationCooldownFromEmpire);
		}
		hero.SetPropertyBaseValue(SimulationProperties.MaximumInfiltrationCooldown, propertyValue);
		this.infiltrationProcesses.Add(infiltrationProcessus);
		hero.Refresh(false);
		this.OnInfiltrationProcessCollectionChange(infiltrationProcessus, CollectionChangeAction.Add);
		this.UpdateInfiltrationCooldown(infiltrationProcessus, false);
		return true;
	}

	private void CreateSpyedGarrison(IGarrison garrison, Unit spy)
	{
		SpiedGarrison spiedGarrison = new SpiedGarrison(garrison.GUID);
		spiedGarrison.Empire = (base.Empire as global::Empire);
		SimulationDescriptor descriptor = null;
		if (this.simulationDescriptorDatabase.TryGetValue("SpiedGarrison", out descriptor))
		{
			spiedGarrison.AddDescriptor(descriptor, false);
		}
		else
		{
			Diagnostics.LogError("Unable to retrieve the 'SpiedGarrison' simulation descriptor from the database.");
		}
		base.Empire.AddChild(spiedGarrison);
		this.spiedGarrisons.Add(spiedGarrison);
		float xp = DepartmentOfIntelligence.ComputeInfiltrationSucceedExperienceGain(base.Empire, garrison, spy);
		spy.GainXp(xp, false, true);
		if (this.EventService != null)
		{
			IGameEntity gameEntity = null;
			if (this.gameEntityRepositoryService.TryGetValue(spy.GUID, out gameEntity))
			{
				Unit unit = gameEntity as Unit;
				if (unit != null)
				{
					this.EventService.Notify(new EventHeroInfiltrated(base.Empire, unit));
				}
			}
		}
		this.departmentOfEducation.InternalChangeAssignment(spy.GUID, spiedGarrison);
	}

	private void OnInfiltrationLevelChange(GameEntityGUID hero, GameEntityGUID garrison)
	{
		if (this.InfiltrationLevelChange != null)
		{
			this.InfiltrationLevelChange(this, new InfiltrationEventArgs(hero, garrison));
		}
	}

	private void OnInfiltrationStateChange(GameEntityGUID hero, GameEntityGUID garrisonGUID)
	{
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(garrisonGUID, out gameEntity))
		{
			InfiltrationProcessus infiltrationProcessus = this.infiltrationProcesses.FirstOrDefault((InfiltrationProcessus infiltrationProcess) => infiltrationProcess.GarrisonGuid == garrisonGUID);
			if (infiltrationProcessus != null && infiltrationProcessus.SpyState == InfiltrationProcessus.InfiltrationState.Infiltrated)
			{
				((City)gameEntity).EmpireInfiltrationBits |= 1 << base.Empire.Index;
			}
			else
			{
				((City)gameEntity).EmpireInfiltrationBits &= ~(1 << base.Empire.Index);
			}
		}
		if (this.InfiltrationStateChange != null)
		{
			this.InfiltrationStateChange(this, new InfiltrationEventArgs(hero, garrisonGUID));
		}
	}

	private void OnInfiltrationProcessCollectionChange(InfiltrationProcessus processus, CollectionChangeAction action)
	{
		if (this.InfiltrationProcessCollectionChange != null)
		{
			this.InfiltrationProcessCollectionChange(this, new CollectionChangeEventArgs(action, processus));
		}
	}

	public bool CanInfiltrateIgnoreVision(Unit hero, IGarrison target, bool fromGround, out float cost, bool silent = true)
	{
		cost = 0f;
		if (!hero.CheckUnitAbility(UnitAbility.ReadonlySpy, -1))
		{
			return false;
		}
		if (DepartmentOfEducation.IsInjured(hero) || DepartmentOfEducation.IsLocked(hero) || DepartmentOfEducation.CheckGarrisonAgainstSiege(hero, hero.Garrison))
		{
			return false;
		}
		if (target.Empire == base.Empire)
		{
			return false;
		}
		if (target is City)
		{
			City city = target as City;
			if (city.BesiegingEmpire != null && city.BesiegingEmpire != base.Empire)
			{
				return false;
			}
		}
		if (hero.Garrison != null && hero.Garrison.IsInEncounter)
		{
			return false;
		}
		if (DepartmentOfIntelligence.IsGarrisonAlreadyUnderInfiltrationProcessus(target.GUID, base.Empire))
		{
			return false;
		}
		int i = 0;
		while (i < this.InfiltrationProcesses.Count)
		{
			if (this.InfiltrationProcesses[i].HeroGuid == hero.GUID)
			{
				if (this.InfiltrationProcesses[i].SpyState == InfiltrationProcessus.InfiltrationState.OnGoing)
				{
					return false;
				}
				break;
			}
			else
			{
				i++;
			}
		}
		if (fromGround)
		{
			if (!this.IsHeroNearGarrison(hero, target))
			{
				return false;
			}
		}
		else
		{
			cost = DepartmentOfIntelligence.ComputeInfiltrationCost(base.Empire, target);
			if (!this.departmentOfTheTreasury.CanAfford(cost, DepartmentOfTheTreasury.Resources.InfiltrationCost))
			{
				cost = -1f;
				return false;
			}
		}
		return true;
	}

	public static StaticString InfiltrationTargetFailureNotVisible = "InfiltrationTargetFailure_NotVisible";

	public static StaticString InfiltrationTargetFailureSiege = "InfiltrationTargetFailure_Siege";

	public static StaticString InfiltrationTargetFailureAlreadyInfiltrated = "InfiltrationTargetFailure_AlreadyInfiltrated";

	public static StaticString InfiltrationTargetFailureNotAffordable = "InfiltrationTargetFailure_NotAffordable";

	public static StaticString InfiltrationTargetFailureCityInfected = "%EspionageLabelAssignSpyCityInfectedDescription";

	private static InterpreterContext infiltrateLevelInterpreterContext;

	private static object[] infiltrateLevelFormulaTokens;

	private static SimulationPath empireSimulationPath = new SimulationPath("../ClassEmpire");

	private static InterpreterContext infiltrationCostInterpreterContext;

	private static object[] infiltrationCostFormulaTokens;

	private static InterpreterContext infiltrationSucceedExperienceGainInterpreterContext;

	private static object[] infiltrationSucceedExperienceGainFormulaTokens;

	private static InterpreterContext infiltrationActionSeniorityGainInterpreterContext;

	private static object[] infiltrationActionSeniorityFormulaTokens;

	private static DepartmentOfIntelligence.AntiSpyResult[] antiSpyResults;

	private static Amplitude.Unity.Framework.AnimationCurve[] antiSpyCurves;

	private List<SpiedGarrison> spiedGarrisons = new List<SpiedGarrison>();

	private List<InfiltrationProcessus> infiltrationProcesses = new List<InfiltrationProcessus>();

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositionningService;

	private IBattleEncounterRepositoryService battleEncounterRepositoryService;

	private IVisibilityService visibilityService;

	private IEndTurnService endTurnService;

	private ILeechService leechService;

	private IDatabase<LeechDefinition> leechDefinitionDatabase;

	private IDatabase<SimulationDescriptor> simulationDescriptorDatabase;

	private IDatabase<InfiltrationAction> infiltrationActionDatabase;

	public enum AntiSpyResult
	{
		Nothing,
		Wounded,
		Injured,
		Captured
	}
}
