using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Path;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[Diagnostics.TagAttribute("Agency")]
[Diagnostics.TagAttribute("Agency")]
[OrderProcessor(typeof(OrderTransferResources), "TransferResources")]
[OrderProcessor(typeof(OrderBailiffReport), "BailiffReport")]
[OrderProcessor(typeof(OrderTransferResourcesByInfiltration), "TransferResourcesBySpy")]
public class DepartmentOfTheTreasury : Agency
{
	public DepartmentOfTheTreasury(global::Empire empire) : base(empire)
	{
		if (DepartmentOfTheTreasury.costReductionDatabase == null)
		{
			DepartmentOfTheTreasury.costReductionDatabase = Databases.GetDatabase<CostReduction>(true);
		}
		if (DepartmentOfTheTreasury.resourceConverterDefinitionDatabase == null)
		{
			DepartmentOfTheTreasury.resourceConverterDefinitionDatabase = Databases.GetDatabase<ResourceConverterDefinition>(true);
		}
		if (DepartmentOfTheTreasury.interpreterContext == null)
		{
			DepartmentOfTheTreasury.interpreterContext = new InterpreterContext(null);
		}
	}

	public event EventHandler<ResourcePropertyChangeEventArgs> ResourcePropertyChange;

	[Service]
	private IEndTurnService EndTurnService { get; set; }

	[Service]
	private IEventService EventService { get; set; }

	private Seizure[] Seizures { get; set; }

	private bool EndTurnValidator(bool force)
	{
		if (this.PlayerControllerRepositoryService.ActivePlayerController.Empire != base.Empire || force)
		{
			return true;
		}
		float num = 0f;
		float num2 = 0f;
		if (!this.TryGetResourceStockValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, true))
		{
			num = 0f;
		}
		if (!this.TryGetNetResourceValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num2, true))
		{
			num2 = 0f;
		}
		if (num2 + num >= 0f)
		{
			return true;
		}
		this.EventService.Notify(new EventBailiffWarning(base.Empire));
		return false;
	}

	private IEnumerator GameServerState_Bailiff(string context, string name)
	{
		if (base.Empire is MajorEmpire && !(base.Empire as MajorEmpire).IsEliminated)
		{
			float num = 0f;
			float num2 = 0f;
			List<AuctionInstruction> instructions = new List<AuctionInstruction>();
			int num3;
			for (int index = 0; index < this.Seizures.Length; index = num3 + 1)
			{
				if (!this.TryGetResourceStockValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, true))
				{
					num = 0f;
				}
				if (!this.TryGetNetResourceValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num2, true))
				{
					num2 = 0f;
				}
				if (num2 + num >= 0f)
				{
					break;
				}
				yield return this.Seizures[index].Execute(base.Empire as global::Empire, instructions);
				num3 = index;
			}
			if (instructions.Count > 0)
			{
				OrderBailiffReport order = new OrderBailiffReport(base.Empire.Index, instructions);
				(Services.GetService<IGameService>().Game.Services.GetService<IPlayerControllerRepositoryService>() as IPlayerControllerRepositoryControl).GetPlayerControllerById("server").PostOrder(order);
			}
			instructions = null;
			instructions = null;
		}
		yield break;
	}

	private void InitializeBailiff()
	{
		this.EventService = Services.GetService<IEventService>();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		Diagnostics.Assert(this.EndTurnService != null);
		this.EndTurnService.RegisterValidator(new Func<bool, bool>(this.EndTurnValidator));
		IDatabase<Seizure> database = Databases.GetDatabase<Seizure>(false);
		this.Seizures = database.GetValues();
		Array.Sort<Seizure>(this.Seizures, (Seizure left, Seizure right) => left.Priority.CompareTo(right.Priority));
		base.Empire.RegisterPass("GameServerState_Bailiff", "Bailiff", new Agency.Action(this.GameServerState_Bailiff), new string[0]);
	}

	private void ReleaseBailiff()
	{
		this.Seizures = new Seizure[0];
		if (this.EndTurnService != null)
		{
			this.EndTurnService.UnregisterValidator(new Func<bool, bool>(this.EndTurnValidator));
			this.EndTurnService = null;
		}
		this.EventService = null;
	}

	public static bool CheckConstructiblePrerequisites(SimulationObject context, ConstructibleElement constructibleElement, params string[] flags)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.Prerequisites == null)
		{
			return true;
		}
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(context))
		{
			for (int i = 0; i < constructibleElement.Prerequisites.Length; i++)
			{
				Prerequisite prerequisite = constructibleElement.Prerequisites[i];
				if (prerequisite != null)
				{
					if (flags == null || flags.Length <= 0 || (prerequisite.Flags != null && Array.Exists<StaticString>(prerequisite.Flags, (StaticString prerequisiteFlag) => Array.Exists<string>(flags, (string flag) => flag == prerequisiteFlag))))
					{
						if (!prerequisite.Check(interpreterSession.Context))
						{
							return false;
						}
					}
				}
			}
		}
		return true;
	}

	public static bool CheckConstructiblePrerequisites(SimulationObject context, ConstructibleElement constructibleElement, ref List<StaticString> failureFlags, params string[] flags)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.Prerequisites == null)
		{
			return true;
		}
		bool result = true;
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(context))
		{
			for (int i = 0; i < constructibleElement.Prerequisites.Length; i++)
			{
				Prerequisite prerequisite = constructibleElement.Prerequisites[i];
				if (prerequisite != null)
				{
					if (flags == null || flags.Length <= 0 || (prerequisite.Flags != null && Array.Exists<StaticString>(prerequisite.Flags, (StaticString prerequisiteFlag) => Array.Exists<string>(flags, (string flag) => flag == prerequisiteFlag))))
					{
						if (!prerequisite.Check(interpreterSession.Context))
						{
							result = false;
							if (prerequisite.Flags != null)
							{
								for (int j = 0; j < prerequisite.Flags.Length; j++)
								{
									failureFlags.AddOnce(prerequisite.Flags[j]);
								}
							}
							else
							{
								Diagnostics.LogWarning("Prerequisite <{0}> are false but there is no associated flags to set in the production state.", new object[]
								{
									prerequisite.ToString()
								});
							}
						}
					}
				}
			}
		}
		return result;
	}

	public static void RegroupConstructionCosts(SimulationObjectWrapper context, ConstructibleElement constructibleElement, bool instant, bool instantOnCompletion, bool applyReduction, ref List<ConstructionCost> finalConstructionCosts, ref ConstructionResourceStock[] constructionResourceStocks)
	{
		for (int i = 0; i < constructibleElement.Costs.Length; i++)
		{
			if (constructionResourceStocks != null)
			{
				constructionResourceStocks[i] = new ConstructionResourceStock(constructibleElement.Costs[i].ResourceName, context);
			}
			if (constructibleElement.Costs[i].Instant == instant && constructibleElement.Costs[i].InstantOnCompletion == instantOnCompletion)
			{
				float num;
				if (applyReduction)
				{
					num = DepartmentOfTheTreasury.GetProductionCostWithBonus(context, constructibleElement, constructibleElement.Costs[i], false);
				}
				else
				{
					num = constructibleElement.Costs[i].GetValue(context);
				}
				if (constructionResourceStocks != null)
				{
					constructionResourceStocks[i].Stock = num;
				}
				if (finalConstructionCosts != null)
				{
					ConstructionCost constructionCost = null;
					for (int j = 0; j < finalConstructionCosts.Count; j++)
					{
						if (finalConstructionCosts[j].ResourceName == constructibleElement.Costs[i].ResourceName)
						{
							constructionCost = finalConstructionCosts[j];
							break;
						}
					}
					if (constructionCost == null)
					{
						constructionCost = new ConstructionCost(constructibleElement.Costs[i].ResourceName, num, constructibleElement.Costs[i].Instant, constructibleElement.Costs[i].InstantOnCompletion);
						finalConstructionCosts.Add(constructionCost);
					}
					else
					{
						constructionCost.Value += num;
					}
				}
			}
		}
	}

	public static void RegroupConstructionCosts(SimulationObjectWrapper context, IConstructionCost[] costs, ref List<ConstructionCost> finalConstructionCosts)
	{
		for (int i = 0; i < costs.Length; i++)
		{
			if (costs[i] != null)
			{
				float value = costs[i].GetValue(context);
				if (finalConstructionCosts != null)
				{
					ConstructionCost constructionCost = null;
					for (int j = 0; j < finalConstructionCosts.Count; j++)
					{
						if (finalConstructionCosts[j].ResourceName == costs[i].ResourceName)
						{
							constructionCost = finalConstructionCosts[j];
							break;
						}
					}
					if (constructionCost == null)
					{
						constructionCost = new ConstructionCost(costs[i].ResourceName, value, costs[i].Instant, costs[i].InstantOnCompletion);
						finalConstructionCosts.Add(constructionCost);
					}
					else
					{
						constructionCost.Value += value;
					}
				}
			}
		}
	}

	public bool CanAfford(float cost, StaticString resourceName)
	{
		DepartmentOfTheTreasury agency = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		float num = -cost;
		return agency.IsTransferOfResourcePossible(base.Empire, resourceName, ref num);
	}

	public bool CanAfford(IConstructionCost[] costs)
	{
		List<ConstructionCost> obj = this.summedConstructionCosts;
		lock (obj)
		{
			this.summedConstructionCosts.Clear();
			DepartmentOfTheTreasury.RegroupConstructionCosts(base.Empire, costs, ref this.summedConstructionCosts);
			int i = 0;
			while (i < this.summedConstructionCosts.Count)
			{
				float num;
				if (this.TryGetResourceStockValue(base.Empire, this.summedConstructionCosts[i].ResourceName, out num, false))
				{
					float value = this.summedConstructionCosts[i].Value;
					if (num >= value)
					{
						i++;
						continue;
					}
				}
				return false;
			}
		}
		return true;
	}

	public List<MissingResource> GetConstructibleMissingRessources(SimulationObjectWrapper context, ConstructibleElement constructibleElement)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.Costs == null)
		{
			return null;
		}
		List<MissingResource> result = new List<MissingResource>();
		this.FillConstructibleMissingRessources(context, constructibleElement, ref result);
		return result;
	}

	public void FillConstructibleMissingRessources(SimulationObjectWrapper context, ConstructibleElement constructibleElement, ref List<MissingResource> missingResourceNeeds)
	{
		List<ConstructionCost> obj = this.summedConstructionCosts;
		lock (obj)
		{
			this.summedConstructionCosts.Clear();
			ConstructionResourceStock[] array = null;
			DepartmentOfTheTreasury.RegroupConstructionCosts(context, constructibleElement, true, false, true, ref this.summedConstructionCosts, ref array);
			for (int i = 0; i < this.summedConstructionCosts.Count; i++)
			{
				float num;
				if (!this.TryGetResourceStockValue(context.SimulationObject, this.summedConstructionCosts[i].ResourceName, out num, false))
				{
					Diagnostics.LogError("Can't check instant resource prerequisite '{1}' for constructible element '{0}'.", new object[]
					{
						constructibleElement.Name,
						constructibleElement.Costs[i].ResourceName
					});
				}
				else
				{
					float value = this.summedConstructionCosts[i].Value;
					if (num < value)
					{
						missingResourceNeeds.Add(new MissingResource
						{
							ResourceName = this.summedConstructionCosts[i].ResourceName,
							AskedResourceValue = value,
							MissingResourceValue = value - num
						});
					}
				}
			}
		}
	}

	public bool CheckConstructibleInstantCosts(SimulationObjectWrapper context, ConstructibleElement constructibleElement)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		if (constructibleElement.Costs == null)
		{
			return true;
		}
		List<ConstructionCost> obj = this.summedConstructionCosts;
		lock (obj)
		{
			this.summedConstructionCosts.Clear();
			ConstructionResourceStock[] array = null;
			DepartmentOfTheTreasury.RegroupConstructionCosts(context, constructibleElement, true, false, true, ref this.summedConstructionCosts, ref array);
			int i = 0;
			while (i < this.summedConstructionCosts.Count)
			{
				float num;
				if (this.TryGetResourceStockValue(context.SimulationObject, this.summedConstructionCosts[i].ResourceName, out num, false))
				{
					float value = this.summedConstructionCosts[i].Value;
					if (num >= value)
					{
						i++;
						continue;
					}
				}
				return false;
			}
		}
		return true;
	}

	public float ComputeConstructionProgress(SimulationObjectWrapper context, Construction construction)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (construction == null)
		{
			throw new ArgumentNullException("construction");
		}
		Diagnostics.Assert(construction.ConstructibleElement != null);
		if (construction.ConstructibleElement.Costs == null)
		{
			return 1f;
		}
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
		{
			Diagnostics.Assert(construction.ConstructibleElement.Costs[i] != null);
			if (!construction.ConstructibleElement.Costs[i].Instant && !construction.ConstructibleElement.Costs[i].InstantOnCompletion)
			{
				Diagnostics.Assert(construction.CurrentConstructionStock != null);
				ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[i];
				Diagnostics.Assert(constructionResourceStock != null);
				num2 += constructionResourceStock.Stock;
				num += DepartmentOfTheTreasury.GetProductionCostWithBonus(context, construction.ConstructibleElement, construction.ConstructibleElement.Costs[i], false);
			}
		}
		if (num < 1.401298E-45f)
		{
			return 1f;
		}
		return num2 / num;
	}

	public int ComputeConstructionRemainingTurn(SimulationObjectWrapper context, Construction construction)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (construction == null)
		{
			throw new ArgumentNullException("construction");
		}
		Diagnostics.Assert(construction.ConstructibleElement != null);
		if (construction.ConstructibleElement.Costs == null)
		{
			return 0;
		}
		int num = 0;
		for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
		{
			Diagnostics.Assert(construction.ConstructibleElement.Costs[i] != null);
			if (!construction.ConstructibleElement.Costs[i].Instant && !construction.ConstructibleElement.Costs[i].InstantOnCompletion)
			{
				Diagnostics.Assert(construction.CurrentConstructionStock != null);
				ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[i];
				Diagnostics.Assert(constructionResourceStock != null);
				float stock = constructionResourceStock.Stock;
				float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(context.SimulationObject, construction.ConstructibleElement, construction.ConstructibleElement.Costs[i], false);
				float num2 = productionCostWithBonus - stock;
				StaticString resourceName = construction.ConstructibleElement.Costs[i].ResourceName;
				float num3;
				if (!this.TryGetResourceStockValue(context, resourceName, out num3, false))
				{
					Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
					{
						resourceName,
						context.Name
					});
				}
				float num4;
				if (!this.TryGetNetResourceValue(context, resourceName, out num4, false))
				{
					return int.MaxValue;
				}
				if (num4 <= 0f)
				{
					return int.MaxValue;
				}
				num += Mathf.CeilToInt((num2 - num3) / num4);
			}
		}
		return num;
	}

	public float ComputeConstructionRemainingCost(SimulationObjectWrapper context, Construction construction, StaticString resourceName)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (construction == null)
		{
			throw new ArgumentNullException("construction");
		}
		Diagnostics.Assert(construction.ConstructibleElement != null);
		if (construction.ConstructibleElement.Costs == null)
		{
			return 0f;
		}
		float num = 0f;
		bool flag = false;
		for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
		{
			IConstructionCost constructionCost = construction.ConstructibleElement.Costs[i];
			Diagnostics.Assert(constructionCost != null);
			if (!constructionCost.Instant && !constructionCost.InstantOnCompletion)
			{
				if (!(constructionCost.ResourceName != resourceName))
				{
					Diagnostics.Assert(construction.CurrentConstructionStock != null);
					ConstructionResourceStock constructionResourceStock = construction.CurrentConstructionStock[i];
					Diagnostics.Assert(constructionResourceStock != null);
					float stock = constructionResourceStock.Stock;
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(context.SimulationObject, construction.ConstructibleElement, constructionCost, false);
					float num2 = productionCostWithBonus - stock;
					num += num2;
					if (!flag)
					{
						float num3;
						if (!this.TryGetResourceStockValue(context, resourceName, out num3, false))
						{
							Diagnostics.LogError("Can't get resource stock value {0} on simulation object {1}.", new object[]
							{
								resourceName,
								context.Name
							});
						}
						num -= num3;
						flag = true;
					}
				}
			}
		}
		return num;
	}

	public void GetConstructibleState(SimulationObjectWrapper context, ConstructibleElement constructibleElement, ref List<StaticString> failureFlags)
	{
		DepartmentOfTheTreasury.CheckConstructiblePrerequisites(context, constructibleElement, ref failureFlags, new string[0]);
		if (failureFlags.Count == 0 && !this.CheckConstructibleInstantCosts(context, constructibleElement))
		{
			failureFlags.AddOnce(ConstructionFlags.InstantCost);
		}
	}

	public bool GetInstantConstructionResourceCostForBuyout(SimulationObjectWrapper context, ConstructibleElement constructibleElement, out ConstructionResourceStock[] constructionResourceStocks)
	{
		return this.TryGetInstantConstructionResourceCost(context, constructibleElement, false, out constructionResourceStocks);
	}

	public bool TryGetInstantConstructionResourceCost(SimulationObjectWrapper context, ConstructibleElement constructibleElement, bool applyReduction, out ConstructionResourceStock[] constructionResourceStocks)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("constructibleElement");
		}
		bool result = true;
		if (constructibleElement.Costs == null || constructibleElement.Costs.Length == 0)
		{
			constructionResourceStocks = new ConstructionResourceStock[0];
		}
		else
		{
			constructionResourceStocks = new ConstructionResourceStock[constructibleElement.Costs.Length];
			List<ConstructionCost> obj = this.summedConstructionCosts;
			lock (obj)
			{
				constructionResourceStocks = new ConstructionResourceStock[constructibleElement.Costs.Length];
				this.summedConstructionCosts.Clear();
				DepartmentOfTheTreasury.RegroupConstructionCosts(context, constructibleElement, true, false, applyReduction, ref this.summedConstructionCosts, ref constructionResourceStocks);
				for (int i = 0; i < this.summedConstructionCosts.Count; i++)
				{
					float value = this.summedConstructionCosts[i].Value;
					float num;
					if (!this.TryGetResourceStockValue(context, this.summedConstructionCosts[i].ResourceName, out num, false))
					{
						Diagnostics.LogError("Constructible element (name: '{0}') asks for instant resource (name: '{1}') that can't be retrieved.", new object[]
						{
							constructibleElement.Name,
							constructibleElement.Costs[i].ResourceName
						});
					}
					else if (value > num)
					{
						result = false;
					}
				}
			}
		}
		return result;
	}

	public static float ComputeCostWithReduction(SimulationObject context, float amount, StaticString costReductionName, CostReduction.ReductionType reductionType)
	{
		float[] obj = DepartmentOfTheTreasury.costReductionValueByPriority;
		float result;
		lock (obj)
		{
			float num = amount;
			CostReduction costReduction;
			if (DepartmentOfTheTreasury.costReductionDatabase.TryGetValue(costReductionName, out costReduction))
			{
				for (int i = 0; i < DepartmentOfTheTreasury.costReductionValueByPriority.Length; i++)
				{
					DepartmentOfTheTreasury.costReductionValueByPriority[i] = 0f;
				}
				DepartmentOfTheTreasury.AddReductionValuePerPriority(costReduction, reductionType, context, ref DepartmentOfTheTreasury.costReductionValueByPriority);
				for (int j = 0; j < DepartmentOfTheTreasury.costReductionValueByPriority.Length; j++)
				{
					num *= 1f - DepartmentOfTheTreasury.costReductionValueByPriority[j];
				}
			}
			result = num;
		}
		return result;
	}

	public static float GetBuyoutCostWithBonus(SimulationObject contextObject, ConstructibleElement constructibleElement)
	{
		if (contextObject == null)
		{
			throw new ArgumentNullException("contextObject");
		}
		if (constructibleElement == null)
		{
			throw new ArgumentNullException("construction");
		}
		SimulationObjectWrapper simulationObjectWrapper = new SimulationObjectWrapper(contextObject);
		float num = 0f;
		Diagnostics.Assert(constructibleElement != null);
		if (constructibleElement.Costs != null)
		{
			for (int i = 0; i < constructibleElement.Costs.Length; i++)
			{
				Diagnostics.Assert(constructibleElement.Costs[i] != null);
				if (!constructibleElement.Costs[i].Instant && !constructibleElement.Costs[i].InstantOnCompletion)
				{
					float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(contextObject, constructibleElement, constructibleElement.Costs[i], false);
					if (productionCostWithBonus > 0f)
					{
						float num2 = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.Buyout, constructibleElement.Costs[i].ResourceName, productionCostWithBonus, simulationObjectWrapper);
						num2 = DepartmentOfTheTreasury.ComputeCostWithReduction(simulationObjectWrapper, constructibleElement, num2, CostReduction.ReductionType.Buyout);
						num += num2;
					}
				}
			}
		}
		return num * (1f + constructibleElement.BuyoutCostModifier);
	}

	public static float GetBuyoutCostWithBonus(SimulationObjectWrapper context, Construction construction)
	{
		if (context == null)
		{
			throw new ArgumentNullException("context");
		}
		if (construction == null)
		{
			throw new ArgumentNullException("construction");
		}
		float num = 0f;
		Diagnostics.Assert(construction.ConstructibleElement != null);
		if (construction.ConstructibleElement.Costs != null)
		{
			Diagnostics.Assert(construction.CurrentConstructionStock != null && construction.ConstructibleElement.Costs.Length == construction.CurrentConstructionStock.Length);
			for (int i = 0; i < construction.ConstructibleElement.Costs.Length; i++)
			{
				Diagnostics.Assert(construction.ConstructibleElement.Costs[i] != null);
				Diagnostics.Assert(construction.CurrentConstructionStock[i] != null);
				if (!construction.ConstructibleElement.Costs[i].Instant && !construction.ConstructibleElement.Costs[i].InstantOnCompletion)
				{
					float num2 = DepartmentOfTheTreasury.GetProductionCostWithBonus(context, construction.ConstructibleElement, construction.ConstructibleElement.Costs[i], false);
					num2 -= construction.CurrentConstructionStock[i].Stock;
					if (num2 > 0f)
					{
						float num3 = DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.Buyout, construction.ConstructibleElement.Costs[i].ResourceName, num2, context);
						num3 = DepartmentOfTheTreasury.ComputeCostWithReduction(context, construction.ConstructibleElement, num3, CostReduction.ReductionType.Buyout);
						num += num3;
					}
				}
			}
		}
		return num * (1f + construction.ConstructibleElement.BuyoutCostModifier);
	}

	public static ResourceDefinition[] GetMigrationCarriedResources()
	{
		if (DepartmentOfTheTreasury.migrationCarriedResources == null)
		{
			IDatabase<ResourceDefinition> database = Databases.GetDatabase<ResourceDefinition>(false);
			Diagnostics.Assert(database != null);
			DepartmentOfTheTreasury.migrationCarriedResources = (from resourceDefinition in database.GetValues()
			where resourceDefinition.CarryOverMigration
			select resourceDefinition).ToArray<ResourceDefinition>();
		}
		return DepartmentOfTheTreasury.migrationCarriedResources;
	}

	public static float GetPopulationBuyOutCost(City city)
	{
		float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
		DepartmentOfTheInterior agency = city.Empire.GetAgency<DepartmentOfTheInterior>();
		float num;
		float num2;
		agency.GetGrowthLimits(propertyValue, out num, out num2);
		float propertyValue2 = city.GetPropertyValue(SimulationProperties.CityGrowthStock);
		float amount = num2 - propertyValue2;
		return DepartmentOfTheTreasury.ConvertCostsTo(DepartmentOfTheTreasury.Resources.PopulationBuyout, SimulationProperties.CityGrowth, amount, city);
	}

	public static float GetProductionCostWithBonus(SimulationObject context, ConstructibleElement constructible, IConstructionCost initialCost, bool silent = false)
	{
		if (context == null)
		{
			if (silent)
			{
				return 0f;
			}
			throw new ArgumentNullException("context");
		}
		else
		{
			if (initialCost == null)
			{
				return 0f;
			}
			float num = initialCost.GetValue(context);
			if (initialCost.ResourceName != DepartmentOfTheTreasury.Resources.Production && initialCost.ResourceName != DepartmentOfTheTreasury.Resources.EmpireResearch && initialCost.ResourceName != DepartmentOfTheTreasury.Resources.EmpirePoint)
			{
				return num;
			}
			if (!initialCost.Instant)
			{
				num = DepartmentOfTheTreasury.ComputeCostWithReduction(context, constructible, num, CostReduction.ReductionType.Production);
				Diagnostics.Assert(num >= 0f);
				num = Mathf.Max(num, 0f);
			}
			return num;
		}
	}

	public static float GetProductionCostWithBonus(SimulationObject context, ConstructibleElement constructible, StaticString resourceName)
	{
		float num = 0f;
		for (int i = 0; i < constructible.Costs.Length; i++)
		{
			if (constructible.Costs[i].ResourceName == resourceName)
			{
				num += DepartmentOfTheTreasury.GetProductionCostWithBonus(context, constructible, constructible.Costs[i], false);
			}
		}
		return num;
	}

	private static float ComputeCostWithReduction(SimulationObject context, ConstructibleElement constructibleElement, float amount, CostReduction.ReductionType type)
	{
		float[] obj = DepartmentOfTheTreasury.costReductionValueByPriority;
		float result;
		lock (obj)
		{
			float num = amount;
			if (constructibleElement != null && constructibleElement.CostReductions != null)
			{
				for (int i = 0; i < DepartmentOfTheTreasury.costReductionValueByPriority.Length; i++)
				{
					DepartmentOfTheTreasury.costReductionValueByPriority[i] = 0f;
				}
				for (int j = 0; j < constructibleElement.CostReductions.Length; j++)
				{
					if (constructibleElement.CostReductions[j] != null)
					{
						DepartmentOfTheTreasury.AddReductionValuePerPriority(constructibleElement.CostReductions[j], type, context, ref DepartmentOfTheTreasury.costReductionValueByPriority);
					}
				}
				for (int k = 0; k < DepartmentOfTheTreasury.costReductionValueByPriority.Length; k++)
				{
					num *= 1f - DepartmentOfTheTreasury.costReductionValueByPriority[k];
				}
			}
			result = num;
		}
		return result;
	}

	private static void AddReductionValuePerPriority(CostReduction costReduction, CostReduction.ReductionType type, SimulationObject context, ref float[] reductionPerPriority)
	{
		if (costReduction.PropertyByType != null)
		{
			for (int i = 0; i < costReduction.PropertyByType.Length; i++)
			{
				if (costReduction.PropertyByType[i].Type == type)
				{
					if (reductionPerPriority.Length < costReduction.PropertyByType[i].Priority + 1)
					{
						Array.Resize<float>(ref reductionPerPriority, costReduction.PropertyByType[i].Priority + 1);
					}
					float propertyValue = context.GetPropertyValue(costReduction.PropertyByType[i].PropertyName);
					reductionPerPriority[costReduction.PropertyByType[i].Priority] = Mathf.Min(1f, reductionPerPriority[costReduction.PropertyByType[i].Priority] + propertyValue);
				}
			}
		}
	}

	private bool BailiffReportPreprocessor(OrderBailiffReport order)
	{
		return order.AuctionInstructions.Count > 0;
	}

	private IEnumerator BailiffReportProcessor(OrderBailiffReport order)
	{
		this.EventService.Notify(new EventBailiffReport(base.Empire, order.AuctionInstructions));
		yield break;
	}

	private bool TransferResourcesPreprocessor(OrderTransferResources order)
	{
		if (string.IsNullOrEmpty(order.ResourceName))
		{
			return false;
		}
		SimulationObjectWrapper simulationObjectWrapper = base.Empire;
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retrieve the game service.");
			return false;
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Cannot retrieve gameEntityRepositoryService.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (order.ContextGUID != GameEntityGUID.Zero && service2.TryGetValue(order.ContextGUID, out gameEntity))
		{
			if (!(gameEntity is SimulationObjectWrapper))
			{
				Diagnostics.LogError("OrderTransferResources preprocessor failed because the context (GUID = '{0}') isn't a SimulationObjectWrapper", new object[]
				{
					order.ContextGUID
				});
				return false;
			}
			simulationObjectWrapper = (gameEntity as SimulationObjectWrapper);
		}
		float amount = order.Amount;
		return this.IsTransferOfResourcePossible(simulationObjectWrapper, order.ResourceName, ref amount);
	}

	private IEnumerator TransferResourcesProcessor(OrderTransferResources order)
	{
		if (order.Amount != 0f)
		{
			SimulationObjectWrapper referenceLocation = base.Empire;
			IGameService gameService = Services.GetService<IGameService>();
			if (gameService == null)
			{
				Diagnostics.LogError("Cannot retrieve the game service.");
				yield break;
			}
			IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
			if (gameEntityRepositoryService == null)
			{
				Diagnostics.LogError("Cannot retrieve gameEntityRepositoryService.");
				yield break;
			}
			IGameEntity gameEntity = null;
			if (order.ContextGUID != GameEntityGUID.Zero && gameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
			{
				if (!(gameEntity is SimulationObjectWrapper))
				{
					Diagnostics.LogError("OrderTransferResources processor failed because the context (GUID = '{0}') isn't a SimulationObject", new object[]
					{
						order.ContextGUID
					});
					yield break;
				}
				referenceLocation = (gameEntity as SimulationObjectWrapper);
			}
			bool result = this.TryTransferResources(referenceLocation, order.ResourceName, order.Amount);
			Diagnostics.Assert(result, "Transfer of resource (name: '{0}', amount: {1}) has failed.", new object[]
			{
				order.ResourceName,
				order.Amount
			});
		}
		yield break;
	}

	private bool TransferResourcesBySpyPreprocessor(OrderTransferResourcesByInfiltration order)
	{
		if (string.IsNullOrEmpty(order.ResourceName))
		{
			return false;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null)
		{
			Diagnostics.LogError("Cannot retrieve the game service.");
			return false;
		}
		IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
		if (service2 == null)
		{
			Diagnostics.LogError("Cannot retrieve gameEntityRepositoryService.");
			return false;
		}
		IGameEntity gameEntity = null;
		if (!service2.TryGetValue(order.ContextGUID, out gameEntity))
		{
			return false;
		}
		City infiltratedCity = gameEntity as City;
		if (infiltratedCity == null)
		{
			return false;
		}
		DepartmentOfIntelligence agency = base.Empire.GetAgency<DepartmentOfIntelligence>();
		if (agency == null || !agency.InfiltrationProcesses.Exists((InfiltrationProcessus match) => match.GarrisonGuid == infiltratedCity.GUID))
		{
			return false;
		}
		bool? flag = new bool?(true);
		InfiltrationAction.ComputeConstructionCost((global::Empire)base.Empire, order, ref flag);
		if (!flag.Value)
		{
			return false;
		}
		float antiSpyParameter;
		order.AntiSpyResult = DepartmentOfIntelligence.GetAntiSpyResult(infiltratedCity, out antiSpyParameter);
		order.AntiSpyParameter = antiSpyParameter;
		DepartmentOfTheTreasury agency2 = infiltratedCity.Empire.GetAgency<DepartmentOfTheTreasury>();
		return agency2.TransferResourcesPreprocessor(order);
	}

	private IEnumerator TransferResourcesBySpyProcessor(OrderTransferResourcesByInfiltration order)
	{
		InfiltrationAction.TryTransferResources((global::Empire)base.Empire, order);
		InfiltrationAction.TryTransferLevelCost(base.Empire as global::Empire, order);
		InfiltrationAction.ApplyExperienceReward(base.Empire as global::Empire, order);
		if (order.Amount != 0f)
		{
			IGameService gameService = Services.GetService<IGameService>();
			if (gameService == null)
			{
				Diagnostics.LogError("Cannot retrieve the game service.");
				yield break;
			}
			IGameEntityRepositoryService gameEntityRepositoryService = gameService.Game.Services.GetService<IGameEntityRepositoryService>();
			if (gameEntityRepositoryService == null)
			{
				Diagnostics.LogError("Cannot retrieve gameEntityRepositoryService.");
				yield break;
			}
			IGameEntity gameEntity = null;
			if (!gameEntityRepositoryService.TryGetValue(order.ContextGUID, out gameEntity))
			{
				yield break;
			}
			City city = gameEntity as City;
			if (city == null)
			{
				yield break;
			}
			DepartmentOfTheTreasury ennemyDepartmentOfTheTreasury = city.Empire.GetAgency<DepartmentOfTheTreasury>();
			yield return ennemyDepartmentOfTheTreasury.TransferResourcesProcessor(order);
			DepartmentOfTheInterior ennemyDepartmentOfTheInterior = city.Empire.GetAgency<DepartmentOfTheInterior>();
			ennemyDepartmentOfTheInterior.ComputeCityPopulation(city, false);
			ennemyDepartmentOfTheInterior.VerifyOverallPopulation(city);
			InfiltrationAction.TryNotifyInfiltrationActionResult((global::Empire)base.Empire, (global::Empire)base.Empire, order);
			InfiltrationAction.TryNotifyInfiltrationActionResult(city.Empire, (global::Empire)base.Empire, order);
		}
		DepartmentOfIntelligence departmentOfIntelligence = base.Empire.GetAgency<DepartmentOfIntelligence>();
		if (departmentOfIntelligence != null)
		{
			departmentOfIntelligence.ExecuteAntiSpy(order.InfiltratedCityGUID, order.AntiSpyResult, order.AntiSpyParameter, false);
		}
		yield break;
	}

	public static float ConvertCostsTo(StaticString to, ConstructibleElement constructible, SimulationObjectWrapper context)
	{
		float num = 0f;
		for (int i = 0; i < constructible.Costs.Length; i++)
		{
			num += DepartmentOfTheTreasury.ConvertCostsTo(to, constructible.Costs[i].ResourceName, constructible.Costs[i].GetValue(context), context);
		}
		return num;
	}

	public static float ConvertCostsTo(StaticString to, Construction construction, SimulationObjectWrapper context)
	{
		float num = 0f;
		for (int i = 0; i < construction.CurrentConstructionStock.Length; i++)
		{
			if (!construction.ConstructibleElement.Costs[i].InstantOnCompletion)
			{
				num += DepartmentOfTheTreasury.ConvertCostsTo(to, construction.CurrentConstructionStock[i].PropertyName, construction.CurrentConstructionStock[i].Stock, context);
			}
		}
		return num;
	}

	public static float ConvertCostsTo(StaticString to, StaticString from, float amount, SimulationObjectWrapper context)
	{
		float result = 0f;
		DepartmentOfTheTreasury.TryConvertCostTo(to, from, amount, context, out result);
		return result;
	}

	public static bool TryConvertCostTo(StaticString to, StaticString from, float amount, SimulationObjectWrapper context, out float result)
	{
		result = 0f;
		ResourceConverterDefinition.ToConverter toConverter = null;
		ResourceConverterDefinition resourceConverterDefinition;
		if (DepartmentOfTheTreasury.resourceConverterDefinitionDatabase.TryGetValue(from, out resourceConverterDefinition) && resourceConverterDefinition.ToConverters != null)
		{
			for (int i = 0; i < resourceConverterDefinition.ToConverters.Length; i++)
			{
				if (resourceConverterDefinition.ToConverters[i].To == to)
				{
					toConverter = resourceConverterDefinition.ToConverters[i];
					break;
				}
			}
			if (toConverter != null)
			{
				if (toConverter.InterpreterModifier != null)
				{
					InterpreterContext obj = DepartmentOfTheTreasury.interpreterContext;
					lock (obj)
					{
						DepartmentOfTheTreasury.interpreterContext.SimulationObject = context;
						DepartmentOfTheTreasury.interpreterContext.Register("Input", amount);
						result = toConverter.InterpreterModifier.GetValue(DepartmentOfTheTreasury.interpreterContext);
					}
				}
				else
				{
					if (toConverter.Modifier == null)
					{
						return false;
					}
					result = toConverter.Modifier.Value * amount;
				}
				return true;
			}
		}
		return false;
	}

	private IPlayerControllerRepositoryService PlayerControllerRepositoryService { get; set; }

	private ITradeManagementService TradeManagementService { get; set; }

	public ResourceDefinition GetAffinityStrategicResource()
	{
		Diagnostics.Assert(this.resourceDatabase != null);
		string text = "Affinity";
		for (int i = 0; i < base.Empire.SimulationObject.Tags.Length; i++)
		{
			string text2 = base.Empire.SimulationObject.Tags[i];
			if (text2.Contains(text))
			{
				StaticString key = text2.Substring(text.Length, text2.Length - text.Length);
				ResourceDefinition result;
				if (this.resourceDatabase.TryGetValue(key, out result))
				{
					return result;
				}
			}
		}
		if (base.Empire.SimulationObject.DescriptorHolders != null)
		{
			for (int j = 0; j < base.Empire.SimulationObject.DescriptorHolders.Count; j++)
			{
				SimulationDescriptorHolder simulationDescriptorHolder = base.Empire.SimulationObject.DescriptorHolders[j];
				Diagnostics.Assert(simulationDescriptorHolder != null);
				string text3 = simulationDescriptorHolder.Descriptor.Name;
				if (text3.Contains(text))
				{
					StaticString key2 = text3.Substring(text.Length, text3.Length - text.Length);
					ResourceDefinition result2;
					if (this.resourceDatabase.TryGetValue(key2, out result2))
					{
						return result2;
					}
				}
			}
		}
		return null;
	}

	public ConstructionCost[] GetUnitHealCost(IEnumerable<Unit> unitToHeal)
	{
		List<ConstructionCost> list = new List<ConstructionCost>();
		foreach (Unit unitToHeal2 in unitToHeal)
		{
			ConstructionCost cost = this.GetUnitHealCost(unitToHeal2);
			ConstructionCost constructionCost = list.Find((ConstructionCost match) => match.ResourceName == cost.ResourceName);
			if (constructionCost != null)
			{
				constructionCost.Value += cost.Value;
			}
			else
			{
				list.Add(cost);
			}
		}
		return list.ToArray();
	}

	public ConstructionCost[] GetUnitForceShiftingCost(IEnumerable<Unit> unitToForceShift)
	{
		List<ConstructionCost> list = new List<ConstructionCost>();
		foreach (Unit unitToForceShift2 in unitToForceShift)
		{
			ConstructionCost cost = this.GetUnitForceShiftingCost(unitToForceShift2);
			ConstructionCost constructionCost = list.Find((ConstructionCost match) => match.ResourceName == cost.ResourceName);
			if (constructionCost != null)
			{
				constructionCost.Value += cost.Value;
			}
			else
			{
				list.Add(cost);
			}
		}
		return list.ToArray();
	}

	public ConstructionCost GetUnitForceShiftingCost(Unit unitToForceShift)
	{
		return new ConstructionCost("Orb", unitToForceShift.GetPropertyValue("LevelDisplayed"), true, true);
	}

	public ConstructionCost GetUnitHealCost(Unit unitToHeal)
	{
		StaticString[] unitAbilityParameters = unitToHeal.GetUnitAbilityParameters(UnitAbility.UnitAbilityInstantHeal);
		ConstructionCost constructionCost;
		if (unitAbilityParameters == null || unitAbilityParameters.Length == 0)
		{
			constructionCost = new ConstructionCost(DepartmentOfTheTreasury.Resources.EmpireMoney, 0f, true, false);
		}
		else
		{
			constructionCost = new ConstructionCost(unitAbilityParameters[0], 0f, true, false);
		}
		float propertyValue = unitToHeal.GetPropertyValue(SimulationProperties.Health);
		float propertyValue2 = unitToHeal.GetPropertyValue(SimulationProperties.MaximumHealth);
		float amount = propertyValue2 - propertyValue;
		constructionCost.Value += DepartmentOfTheTreasury.ConvertCostsTo(constructionCost.ResourceName, SimulationProperties.Health, amount, base.Empire);
		return constructionCost;
	}

	public float GetRovingClansTollFee(TradableTransaction transaction)
	{
		if (!DepartmentOfTheInterior.CanCollectTollFeeOnTransactions(base.Empire as global::Empire))
		{
			return 0f;
		}
		if ((ulong)transaction.EmpireIndex == (ulong)((long)base.Empire.Index))
		{
			return 0f;
		}
		float num = 0f;
		float num2 = 0f;
		TradableTransactionType type = transaction.Type;
		if (type != TradableTransactionType.Buyout)
		{
			if (type == TradableTransactionType.Sellout)
			{
				num = base.Empire.GetPropertyValue(SimulationProperties.TradersSelloutMultiplier);
				float selloutMultiplier = Tradable.SelloutMultiplier;
				if (selloutMultiplier > 0f)
				{
					num2 = Math.Abs(transaction.Price - transaction.Price / selloutMultiplier);
				}
			}
		}
		else
		{
			num = base.Empire.GetPropertyValue(SimulationProperties.TradersBuyoutMultiplier);
			float buyoutMultiplier = Tradable.BuyoutMultiplier;
			if (buyoutMultiplier > 0f)
			{
				num2 = Math.Abs(transaction.Price - transaction.Price / buyoutMultiplier);
			}
		}
		if (ELCPUtilities.UseELCPSymbiosisBuffs && (base.Empire as global::Empire).SimulationObject.Tags.Contains(DepartmentOfTheInterior.RovingClansIntegrationDescriptor1))
		{
			num += 0.08f;
		}
		return num2 * num;
	}

	public bool IsTransferOfResourcePossible(SimulationObject referenceLocation, StaticString resourceName, ref float amount)
	{
		if (referenceLocation == null)
		{
			throw new ArgumentNullException("referenceLocation");
		}
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
			{
				resourceName,
				referenceLocation.Name
			});
			return false;
		}
		Diagnostics.Assert(resourceDefinition != null);
		SimulationObject referenceLocation2 = null;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out referenceLocation2, out resourceLocationDefinition))
		{
			Diagnostics.LogWarning("Can't find resource location for resource '{0}' with base location at '{1}'.", new object[]
			{
				resourceName,
				referenceLocation.Name
			});
			return false;
		}
		return this.IsTransferOfResourcePossible(referenceLocation2, resourceDefinition, resourceLocationDefinition, ref amount);
	}

	public ResourceDefinition.Type GetResourceType(StaticString resourceName)
	{
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		bool condition = this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition);
		Diagnostics.Assert(condition, "Could not find resourceDefinition for resource {0}", new object[]
		{
			resourceName
		});
		return resourceDefinition.ResourceType;
	}

	public bool TryGetNetResourceName(SimulationObject referenceLocation, StaticString resourceName, out StaticString name, bool silent = false)
	{
		if (referenceLocation == null)
		{
			throw new ArgumentNullException("referenceLocation");
		}
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		name = StaticString.Empty;
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		SimulationObject simulationObject;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out simulationObject, out resourceLocationDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource location '{0}'.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		name = resourceLocationDefinition.NetResourcePropertyName;
		return true;
	}

	public bool TryGetNetResourceValue(SimulationObject referenceLocation, StaticString resourceName, out float value, bool silent = false)
	{
		if (referenceLocation == null)
		{
			throw new ArgumentNullException("referenceLocation");
		}
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		value = 0f;
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		SimulationObject simulationObject;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out simulationObject, out resourceLocationDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource location '{0}'.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		Diagnostics.Assert(resourceDefinition != null);
		SimulationProperty property = simulationObject.GetProperty(resourceLocationDefinition.NetResourcePropertyName);
		if (property == null)
		{
			Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
			{
				resourceLocationDefinition.NetResourcePropertyName,
				simulationObject.Name
			});
			return false;
		}
		value = property.Value;
		return true;
	}

	public bool TryGetResourceStockValue(SimulationObject referenceLocation, StaticString resourceName, out float value, bool silent = false)
	{
		if (referenceLocation == null)
		{
			throw new ArgumentNullException("referenceLocation");
		}
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		value = 0f;
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		SimulationObject location;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out location, out resourceLocationDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource location '{0}' (reference location: {1}).", new object[]
				{
					resourceName,
					referenceLocation.Name
				});
			}
			return false;
		}
		SimulationProperty stockProperty = this.GetStockProperty(location, resourceDefinition, resourceLocationDefinition);
		if (stockProperty == null)
		{
			return false;
		}
		value = stockProperty.Value;
		return true;
	}

	public bool TryGetResourceMaximumStockValue(SimulationObject referenceLocation, StaticString resourceName, out float value, bool silent = false)
	{
		if (referenceLocation == null)
		{
			throw new ArgumentNullException("referenceLocation");
		}
		if (StaticString.IsNullOrEmpty(resourceName))
		{
			throw new ArgumentNullException("resourceName");
		}
		value = float.MaxValue;
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
				{
					resourceName
				});
			}
			return false;
		}
		SimulationObject location;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out location, out resourceLocationDefinition))
		{
			if (!silent)
			{
				Diagnostics.LogWarning("Can't find resource location '{0}' (reference location: {1}).", new object[]
				{
					resourceName,
					referenceLocation.Name
				});
			}
			return false;
		}
		SimulationProperty maximumStockProperty = this.GetMaximumStockProperty(location, resourceDefinition, resourceLocationDefinition);
		if (maximumStockProperty == null)
		{
			return false;
		}
		value = maximumStockProperty.Value;
		return true;
	}

	public bool TryTransferResources(SimulationObject referenceLocation, StaticString resourceName, float amount)
	{
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition resourceDefinition;
		if (!this.resourceDatabase.TryGetValue(resourceName, out resourceDefinition))
		{
			Diagnostics.LogWarning("Can't find resource '{0}' in resource database.", new object[]
			{
				resourceName
			});
			return false;
		}
		SimulationObject location;
		ResourceLocationDefinition resourceLocationDefinition;
		if (!this.TryToFindResourceLocation(referenceLocation, resourceDefinition, out location, out resourceLocationDefinition))
		{
			Diagnostics.LogWarning("Can't find resource location '{0}'.", new object[]
			{
				resourceName
			});
			return false;
		}
		return this.TransferResources(location, resourceDefinition, resourceLocationDefinition, amount);
	}

	protected override IEnumerator OnInitialize()
	{
		yield return base.OnInitialize();
		this.resourceDatabase = Databases.GetDatabase<ResourceDefinition>(true);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.PlayerControllerRepositoryService = gameService.Game.Services.GetService<IPlayerControllerRepositoryService>();
		Diagnostics.Assert(base.Empire != null);
		base.Empire.RegisterPass("GameClientState_Turn_End", "CollectResources", new Agency.Action(this.GameClientState_Turn_End_CollectResources), new string[]
		{
			"RefreshApprovalStatus",
			"UpdateAlliancePrestigeTrendBonus",
			"UpdateInfiltration"
		});
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ClampResourceStocks", new Agency.Action(this.GameClientState_Turn_Begin_ClampResourceStocks), new string[0]);
		base.Empire.RegisterPass("GameClientState_Turn_Begin", "ApplyResourceSimulationEffects", new Agency.Action(this.GameClientState_Turn_Begin_ApplyResourceSimulationEffects), new string[0]);
		global::Game game = gameService.Game as global::Game;
		if (game == null)
		{
			Diagnostics.LogError("Can't retrieve game instance.");
			yield break;
		}
		Dictionary<string, int> depositsCount = new Dictionary<string, int>();
		Diagnostics.Assert(game.World != null);
		Region[] regions = game.World.Regions;
		Diagnostics.Assert(regions != null);
		foreach (Region region in regions)
		{
			Diagnostics.Assert(region != null);
			PointOfInterest[] pointOfInterests = region.PointOfInterests;
			if (pointOfInterests != null)
			{
				foreach (PointOfInterest pointOfInterest in pointOfInterests)
				{
					Diagnostics.Assert(pointOfInterest != null);
					SimulationDescriptor typeDescriptor = pointOfInterest.GetDescriptorFromType(PointOfInterest.PointOfInterestType);
					if (typeDescriptor != null && !(typeDescriptor.Name != "PointOfInterestTypeResourceDeposit"))
					{
						Diagnostics.Assert(pointOfInterest.PointOfInterestDefinition != null);
						string pointOfInterestResourceName;
						if (pointOfInterest.PointOfInterestDefinition.TryGetValue("ResourceName", out pointOfInterestResourceName))
						{
							Diagnostics.Assert(!string.IsNullOrEmpty(pointOfInterestResourceName));
							if (!depositsCount.ContainsKey(pointOfInterestResourceName))
							{
								depositsCount.Add(pointOfInterestResourceName, 0);
							}
							Dictionary<string, int> dictionary2;
							Dictionary<string, int> dictionary = dictionary2 = depositsCount;
							string key2;
							string key = key2 = pointOfInterestResourceName;
							int num = dictionary2[key2];
							dictionary[key] = num + 1;
						}
					}
				}
			}
		}
		foreach (KeyValuePair<string, int> keyValuePair in depositsCount)
		{
			Diagnostics.Assert(this.resourceDatabase != null);
			ResourceDefinition resourceDefinition;
			if (!this.resourceDatabase.TryGetValue(keyValuePair.Key, out resourceDefinition))
			{
				Diagnostics.LogWarning("Unable to retrieve the resource definition '{0}' from the database.", new object[]
				{
					keyValuePair.Key
				});
			}
			else
			{
				Diagnostics.Assert(resourceDefinition != null);
				Diagnostics.Assert(base.Empire != null);
				SimulationObject location;
				ResourceLocationDefinition resourceLocationDefinition;
				if (this.TryToFindResourceLocation(base.Empire.SimulationObject, resourceDefinition, out location, out resourceLocationDefinition))
				{
					Diagnostics.Assert(location != null);
					SimulationProperty resourceCountProperty = location.GetProperty(resourceLocationDefinition.ResourceCountPropertyName);
					if (resourceCountProperty != null)
					{
						Diagnostics.Assert(resourceCountProperty.PropertyDescriptor != null);
						if (!resourceCountProperty.PropertyDescriptor.IsSealed)
						{
							Diagnostics.LogWarning("The property {0} must be a sealed property.", new object[]
							{
								resourceLocationDefinition.ResourceCountPropertyName
							});
						}
						else
						{
							location.SetPropertyBaseValue(resourceLocationDefinition.ResourceCountPropertyName, (float)keyValuePair.Value);
						}
					}
				}
			}
		}
		this.InitializeBailiff();
		Diagnostics.Assert(base.Empire != null);
		base.Empire.Refresh(false);
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
		this.TradeManagementService = game.Services.GetService<ITradeManagementService>();
		if (this.TradeManagementService != null)
		{
			this.TradeManagementService.TransactionComplete += this.TradeManagementService_TransactionComplete;
		}
		if (ELCPUtilities.SpectatorMode && base.Empire.SimulationObject.Tags.Contains(global::Empire.TagEmpireEliminated))
		{
			global::Empire[] empires = ((global::Game)game).Empires;
			for (int i = 0; i < empires.Length; i++)
			{
				MajorEmpire majorEmpire = empires[i] as MajorEmpire;
				if (majorEmpire == null)
				{
					break;
				}
				if (majorEmpire.Index != base.Empire.Index && !majorEmpire.IsEliminated)
				{
					majorEmpire.ArmiesInfiltrationBits |= 1 << base.Empire.Index;
					DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
					foreach (City city in agency.Cities)
					{
						city.EmpireInfiltrationBits |= 1 << base.Empire.Index;
					}
					foreach (Fortress fortress in agency.OccupiedFortresses)
					{
						fortress.PointOfInterest.InfiltrationBits |= 1 << base.Empire.Index;
					}
					foreach (Village village in agency.ConvertedVillages)
					{
						village.PointOfInterest.InfiltrationBits |= 1 << base.Empire.Index;
					}
				}
			}
		}
		yield break;
	}

	protected override void OnRelease()
	{
		base.OnRelease();
		if (DepartmentOfTheTreasury.costReductionDatabase != null)
		{
			DepartmentOfTheTreasury.costReductionDatabase = null;
		}
		if (DepartmentOfTheTreasury.resourceConverterDefinitionDatabase != null)
		{
			DepartmentOfTheTreasury.resourceConverterDefinitionDatabase = null;
		}
		if (DepartmentOfTheTreasury.interpreterContext != null)
		{
			DepartmentOfTheTreasury.interpreterContext.SimulationObject = null;
			DepartmentOfTheTreasury.interpreterContext.Clear();
			DepartmentOfTheTreasury.interpreterContext = null;
		}
		this.ReleaseBailiff();
		this.resourceDatabase = null;
		if (this.TradeManagementService != null)
		{
			this.TradeManagementService.TransactionComplete -= this.TradeManagementService_TransactionComplete;
			this.TradeManagementService = null;
		}
	}

	private IEnumerator GameClientState_Turn_Begin_ApplyResourceSimulationEffects(string context, string name)
	{
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition[] resources = this.resourceDatabase.GetValues();
		if (resources == null)
		{
			yield break;
		}
		foreach (ResourceDefinition resourceDefinition in resources)
		{
			if (resourceDefinition.ResourceType != ResourceDefinition.Type.Gameplay && resourceDefinition.ResourceType != ResourceDefinition.Type.StaticAlias)
			{
				Diagnostics.Assert(resourceDefinition != null);
				if (resourceDefinition.SimulationEffects != null)
				{
					Diagnostics.Assert(resourceDefinition.ResourceLocationDefinitions != null);
					for (int locationPathIndex = 0; locationPathIndex < resourceDefinition.ResourceLocationDefinitions.Length; locationPathIndex++)
					{
						SimulationObject[] locations = resourceDefinition.ResourceLocationDefinitions[locationPathIndex].LocationPath.GetValidatedObjects(base.Empire, PathNavigatorSemantic.CheckValidity);
						Diagnostics.Assert(locations != null);
						foreach (SimulationObject location in locations)
						{
							Diagnostics.Assert(location != null);
							location.Refresh();
							for (int effectIndex = 0; effectIndex < resourceDefinition.SimulationEffects.Length; effectIndex++)
							{
								Diagnostics.Assert(resourceDefinition.SimulationEffects[effectIndex] != null);
								resourceDefinition.SimulationEffects[effectIndex].Execute(location);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_Begin_ClampResourceStocks(string context, string name)
	{
		Diagnostics.Assert(this.resourceDatabase != null);
		ResourceDefinition[] resources = this.resourceDatabase.GetValues();
		if (resources == null)
		{
			yield break;
		}
		foreach (ResourceDefinition resourceDefinition in resources)
		{
			if (resourceDefinition.ResourceType != ResourceDefinition.Type.Gameplay && resourceDefinition.ResourceType != ResourceDefinition.Type.StaticAlias)
			{
				Diagnostics.Assert(resourceDefinition.ResourceLocationDefinitions != null);
				for (int locationPathIndex = 0; locationPathIndex < resourceDefinition.ResourceLocationDefinitions.Length; locationPathIndex++)
				{
					ResourceLocationDefinition locationDefinition = resourceDefinition.ResourceLocationDefinitions[locationPathIndex];
					foreach (SimulationObject location in locationDefinition.LocationPath.GetValidatedObjects(base.Empire, PathNavigatorSemantic.CheckValidity))
					{
						location.Refresh();
						SimulationProperty resourceStockProperty = location.GetProperty(locationDefinition.ResourceStockPropertyName);
						if (resourceStockProperty == null)
						{
							Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
							{
								locationDefinition.ResourceStockPropertyName,
								location.Name
							});
						}
						else
						{
							SimulationProperty minimumResourceStockProperty = location.GetProperty(locationDefinition.MinimumResourceStockPropertyName);
							SimulationProperty maximumResourceStockProperty = location.GetProperty(locationDefinition.MaximumResourceStockPropertyName);
							float minimumResourceStock = (minimumResourceStockProperty == null) ? float.NegativeInfinity : minimumResourceStockProperty.Value;
							float maximumResourceStock = (maximumResourceStockProperty == null) ? float.PositiveInfinity : maximumResourceStockProperty.Value;
							float newResourceStock = Mathf.Clamp(resourceStockProperty.Value, minimumResourceStock, maximumResourceStock);
							location.SetPropertyBaseValue(locationDefinition.ResourceStockPropertyName, newResourceStock);
							location.Refresh();
							this.OnResourcePropertyChange(resourceDefinition, locationDefinition.ResourceStockPropertyName, location, -resourceStockProperty.Value + newResourceStock);
						}
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator GameClientState_Turn_End_CollectResources(string context, string name)
	{
		ResourceDefinition[] resources = this.resourceDatabase.GetValues();
		if (resources == null)
		{
			yield break;
		}
		foreach (ResourceDefinition resourceDefinition in resources)
		{
			if (resourceDefinition.ResourceType != ResourceDefinition.Type.Gameplay && resourceDefinition.ResourceType != ResourceDefinition.Type.StaticAlias)
			{
				Diagnostics.Assert(resourceDefinition.ResourceLocationDefinitions != null);
				for (int locationPathIndex = 0; locationPathIndex < resourceDefinition.ResourceLocationDefinitions.Length; locationPathIndex++)
				{
					ResourceLocationDefinition locationDefinition = resourceDefinition.ResourceLocationDefinitions[locationPathIndex];
					foreach (SimulationObject location in locationDefinition.LocationPath.GetValidatedObjects(base.Empire, PathNavigatorSemantic.CheckValidity))
					{
						location.Refresh();
						SimulationProperty resourceNetProperty = location.GetProperty(locationDefinition.NetResourcePropertyName);
						if (resourceNetProperty == null)
						{
							Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
							{
								locationDefinition.NetResourcePropertyName,
								location.Name
							});
						}
						else
						{
							SimulationProperty resourceStockProperty = location.GetProperty(locationDefinition.ResourceStockPropertyName);
							if (resourceStockProperty == null)
							{
								Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
								{
									locationDefinition.ResourceStockPropertyName,
									location.Name
								});
							}
							else
							{
								float newResourceStock = resourceStockProperty.Value + resourceNetProperty.Value;
								location.SetPropertyBaseValue(locationDefinition.ResourceStockPropertyName, newResourceStock);
								if (!StaticString.IsNullOrEmpty(locationDefinition.AccumulatorPropertyName) && resourceNetProperty.Value > 0f)
								{
									SimulationProperty accumulator = location.GetProperty(locationDefinition.AccumulatorPropertyName);
									if (accumulator != null)
									{
										Diagnostics.Assert(accumulator.PropertyDescriptor != null);
										if (!accumulator.PropertyDescriptor.IsSealed)
										{
											Diagnostics.LogWarning("The property '{0}' must be a sealed property.", new object[]
											{
												accumulator.Name
											});
										}
										location.SetPropertyBaseValue(locationDefinition.AccumulatorPropertyName, accumulator.Value + resourceNetProperty.Value);
									}
								}
								location.Refresh();
								this.OnResourcePropertyChange(resourceDefinition, locationDefinition.ResourceStockPropertyName, location, resourceNetProperty.Value);
							}
						}
					}
				}
			}
		}
		yield break;
	}

	private SimulationProperty GetStockProperty(SimulationObject location, ResourceDefinition resourceDefinition, ResourceLocationDefinition resourceLocationDefinition)
	{
		Diagnostics.Assert(resourceLocationDefinition != null);
		Diagnostics.Assert(location != null);
		SimulationProperty property = location.GetProperty(resourceLocationDefinition.ResourceStockPropertyName);
		if (property == null)
		{
			Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
			{
				resourceLocationDefinition.ResourceStockPropertyName,
				location.Name
			});
			return null;
		}
		return property;
	}

	private SimulationProperty GetMaximumStockProperty(SimulationObject location, ResourceDefinition resourceDefinition, ResourceLocationDefinition resourceLocationDefinition)
	{
		Diagnostics.Assert(resourceLocationDefinition != null);
		Diagnostics.Assert(location != null);
		return location.GetProperty(resourceLocationDefinition.MaximumResourceStockPropertyName);
	}

	private bool IsTransferOfResourcePossible(SimulationObject referenceLocation, ResourceDefinition resourceDefinition, ResourceLocationDefinition resourceLocationDefinition, ref float amount)
	{
		SimulationProperty property = referenceLocation.GetProperty(resourceLocationDefinition.ResourceStockPropertyName);
		if (property == null)
		{
			Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
			{
				resourceLocationDefinition.ResourceStockPropertyName,
				referenceLocation.Name
			});
			return false;
		}
		SimulationProperty property2 = referenceLocation.GetProperty(resourceLocationDefinition.MinimumResourceStockPropertyName);
		SimulationProperty property3 = referenceLocation.GetProperty(resourceLocationDefinition.MaximumResourceStockPropertyName);
		float num = (property2 == null) ? float.NegativeInfinity : property2.Value;
		float num2 = (property3 == null) ? float.PositiveInfinity : property3.Value;
		if (amount < 0f)
		{
			return property.Value + amount >= num;
		}
		float num3 = amount;
		amount = Math.Max(0f, Math.Min(amount, num2 - property.Value));
		if (amount == 0f)
		{
			amount = num3;
		}
		return true;
	}

	private void OnResourcePropertyChange(ResourceDefinition resourceDefinition, StaticString resourcePropertyName, SimulationObject location, float amount)
	{
		if (this.ResourcePropertyChange != null)
		{
			this.ResourcePropertyChange(this, new ResourcePropertyChangeEventArgs(resourceDefinition, resourcePropertyName, location, amount));
		}
	}

	private void TradeManagementService_TransactionComplete(object sender, TradableTransactionCompleteEventArgs e)
	{
		if ((ulong)e.Transaction.EmpireIndex == (ulong)((long)base.Empire.Index) || !DepartmentOfTheInterior.CanCollectTollFeeOnTransactions(base.Empire as global::Empire))
		{
			return;
		}
		this.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, this.GetRovingClansTollFee(e.Transaction));
	}

	private bool TransferResources(SimulationObject location, ResourceDefinition resourceDefinition, ResourceLocationDefinition resourceLocationDefinition, float amount)
	{
		if (!this.IsTransferOfResourcePossible(location, resourceDefinition, resourceLocationDefinition, ref amount))
		{
			return false;
		}
		SimulationProperty property = location.GetProperty(resourceLocationDefinition.ResourceStockPropertyName);
		if (property == null)
		{
			Diagnostics.LogWarning("Property '{0}' does not exist in the simulation object '{1}'.", new object[]
			{
				resourceLocationDefinition.ResourceStockPropertyName,
				location.Name
			});
			return false;
		}
		Diagnostics.Assert(property.PropertyDescriptor != null);
		if (!property.PropertyDescriptor.IsSealed)
		{
			Diagnostics.LogWarning("The property '{0}' must be a sealed property.", new object[]
			{
				resourceLocationDefinition.ResourceStockPropertyName
			});
			return false;
		}
		location.SetPropertyBaseValue(resourceLocationDefinition.ResourceStockPropertyName, property.Value + amount);
		if (!StaticString.IsNullOrEmpty(resourceLocationDefinition.AccumulatorPropertyName) && amount > 0f)
		{
			SimulationProperty property2 = location.GetProperty(resourceLocationDefinition.AccumulatorPropertyName);
			if (property2 != null)
			{
				Diagnostics.Assert(property2.PropertyDescriptor != null);
				if (!property2.PropertyDescriptor.IsSealed)
				{
					Diagnostics.LogWarning("The property '{0}' must be a sealed property.", new object[]
					{
						property2.Name
					});
				}
				location.SetPropertyBaseValue(resourceLocationDefinition.AccumulatorPropertyName, property2.Value + amount);
			}
		}
		location.Refresh();
		this.OnResourcePropertyChange(resourceDefinition, resourceLocationDefinition.ResourceStockPropertyName, location, amount);
		return true;
	}

	private bool TryToFindResourceLocation(SimulationObject referenceLocation, ResourceDefinition resourceDefinition, out SimulationObject location, out ResourceLocationDefinition resourceLocationDefinition)
	{
		location = null;
		resourceLocationDefinition = null;
		SimulationObject simulationObject = null;
		ResourceLocationDefinition resourceLocationDefinition2 = null;
		Diagnostics.Assert(resourceDefinition.ResourceLocationDefinitions != null);
		for (int i = 0; i < resourceDefinition.ResourceLocationDefinitions.Length; i++)
		{
			ResourceLocationDefinition resourceLocationDefinition3 = resourceDefinition.ResourceLocationDefinitions[i];
			Diagnostics.Assert(resourceLocationDefinition3 != null && resourceLocationDefinition3.LocationPath != null);
			SimulationObject[] validatedObjects = resourceLocationDefinition3.LocationPath.GetValidatedObjects(base.Empire, PathNavigatorSemantic.CheckValidity);
			if (validatedObjects != null && validatedObjects.Length != 0)
			{
				if (validatedObjects.Length == 1 && simulationObject == null)
				{
					simulationObject = validatedObjects[0];
					resourceLocationDefinition2 = resourceLocationDefinition3;
					if (simulationObject != referenceLocation)
					{
						goto IL_E6;
					}
					location = simulationObject;
				}
				else
				{
					if (!Array.Exists<SimulationObject>(validatedObjects, (SimulationObject validLocation) => validLocation == referenceLocation))
					{
						goto IL_E6;
					}
					location = referenceLocation;
				}
				resourceLocationDefinition = resourceLocationDefinition3;
				return true;
			}
			IL_E6:;
		}
		if (simulationObject != null)
		{
			location = simulationObject;
			resourceLocationDefinition = resourceLocationDefinition2;
			return true;
		}
		return false;
	}

	internal virtual void OnEmpireEliminated(global::Empire empire, bool authorized)
	{
		if (empire.Index == base.Empire.Index)
		{
			float num;
			if (this.TryGetResourceStockValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, true) && num > 0f)
			{
				this.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpireMoney, -num);
			}
			if (this.TryGetResourceStockValue(base.Empire, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, true) && num > 0f)
			{
				this.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.EmpirePoint, -num);
			}
			if (this.TryGetResourceStockValue(base.Empire, DepartmentOfTheTreasury.Resources.Orb, out num, true) && num > 0f)
			{
				this.TryTransferResources(base.Empire, DepartmentOfTheTreasury.Resources.Orb, -num);
			}
			for (int i = 1; i < 7; i++)
			{
				if (this.TryGetResourceStockValue(base.Empire, "Strategic" + i, out num, true) && num > 0f)
				{
					this.TryTransferResources(base.Empire, "Strategic" + i, -num);
				}
			}
			for (int j = 1; j < 16; j++)
			{
				if (this.TryGetResourceStockValue(base.Empire, "Luxury" + j, out num, true) && num > 0f)
				{
					this.TryTransferResources(base.Empire, "Luxury" + j, -num);
				}
			}
			IGameService service = Services.GetService<IGameService>();
			if (!ELCPUtilities.SpectatorMode || service == null || service.Game == null)
			{
				return;
			}
			global::Game game = service.Game as global::Game;
			if (game == null)
			{
				return;
			}
			global::Empire[] empires = game.Empires;
			for (int k = 0; k < empires.Length; k++)
			{
				MajorEmpire majorEmpire = empires[k] as MajorEmpire;
				if (majorEmpire == null)
				{
					break;
				}
				if (majorEmpire.Index != base.Empire.Index && !majorEmpire.IsEliminated)
				{
					majorEmpire.ArmiesInfiltrationBits |= 1 << base.Empire.Index;
					DepartmentOfTheInterior agency = majorEmpire.GetAgency<DepartmentOfTheInterior>();
					foreach (City city in agency.Cities)
					{
						city.EmpireInfiltrationBits |= 1 << base.Empire.Index;
					}
					foreach (Fortress fortress in agency.OccupiedFortresses)
					{
						fortress.PointOfInterest.InfiltrationBits |= 1 << base.Empire.Index;
					}
					foreach (Village village in agency.ConvertedVillages)
					{
						village.PointOfInterest.InfiltrationBits |= 1 << base.Empire.Index;
					}
				}
			}
		}
	}

	private List<ConstructionCost> summedConstructionCosts = new List<ConstructionCost>();

	private static IDatabase<CostReduction> costReductionDatabase;

	private static IDatabase<ResourceConverterDefinition> resourceConverterDefinitionDatabase;

	private static ResourceDefinition[] migrationCarriedResources;

	private static float[] costReductionValueByPriority = new float[1];

	private static InterpreterContext interpreterContext;

	private IDatabase<ResourceDefinition> resourceDatabase;

	public static class Resources
	{
		public static readonly StaticString ActionPoint = "ActionPoint";

		public static readonly StaticString QueuedFreeBorough = "QueuedFreeBorough";

		public static readonly StaticString FreeBorough = "FreeBorough";

		public static readonly StaticString CityGrowth = "CityGrowth";

		public static readonly StaticString Production = "Production";

		public static readonly StaticString EmpireMoney = "EmpireMoney";

		public static readonly StaticString EmpirePoint = "EmpirePoint";

		public static readonly StaticString EmpireResearch = "EmpireResearch";

		public static readonly StaticString Cadaver = "Cadaver";

		public static readonly StaticString Lavapool = "Lavapool";

		public static readonly StaticString SiegeDamage = "SiegeDamage";

		public static readonly StaticString PeacePoint = "EmpirePeacePoint";

		public static readonly StaticString Orb = "Orb";

		public static readonly StaticString PopulationBuyout = "PopulationBuyout";

		public static readonly StaticString TechnologiesBuyOut = "TechnologiesBuyOut";

		public static readonly StaticString Buyout = "Buyout";

		public static readonly StaticString Refund = "Refund";

		public static readonly StaticString InfiltrationPoint = "InfiltrationPoint";

		public static readonly StaticString InfiltrationCost = "InfiltrationCost";

		public static readonly StaticString Luxuries = "LuxuryResource";
	}
}
