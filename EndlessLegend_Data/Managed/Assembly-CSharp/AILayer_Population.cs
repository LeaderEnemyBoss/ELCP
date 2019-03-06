using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[Diagnostics.TagAttribute("AI")]
public class AILayer_Population : AILayer
{
	protected Empire Empire
	{
		get
		{
			return base.AIEntity.Empire;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		this.aiEntityCity = (aiEntity as AIEntity_City);
		this.amasCityLayer = this.aiEntityCity.GetLayer<AILayer_AmasCity>();
		yield return base.Initialize(aiEntity);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "LayerPopulation_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"LayerAmasCity_CreateLocalNeedsPass",
			"LayerExtension_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "LayerPopulation_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[]
		{
			"LayerExtension_EvaluateNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "LayerPopulation_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[]
		{
			"LayerExtension_ExecuteNeedsPass"
		});
		this.interpreterContext = new InterpreterContext(this.aiEntityCity.City);
		for (int index = 0; index < AILayer_Population.PopulationResource.Length; index++)
		{
			this.interpreterContext.Register(AILayer_Population.PopulationResource[index], 1);
		}
		this.preferedPopulationMessageID = base.AIEntity.AIPlayer.Blackboard.AddMessage(new PreferedPopulationMessage(this.aiEntityCity.City.GUID));
		if (!(this.aiEntityCity.AIPlayer is AIPlayer_MajorEmpire))
		{
			AILayer.LogError("The agent context object is not an ai player.");
		}
		DepartmentOfIndustry industry = this.Empire.GetAgency<DepartmentOfIndustry>();
		DepartmentOfIndustry.ConstructibleElement[] constructibleElements = ((IConstructibleElementDatabase)industry).GetAvailableConstructibleElements(new StaticString[]
		{
			DistrictImprovementDefinition.ReadOnlyCategory
		});
		Diagnostics.Assert(constructibleElements != null);
		this.districtImprovement = Array.Find<DepartmentOfIndustry.ConstructibleElement>(constructibleElements, delegate(DepartmentOfIndustry.ConstructibleElement element)
		{
			AIPlayer_MajorEmpire aiPlayer;
			return element.Name == aiPlayer.AIData_Faction.DistrictImprovement;
		});
		Diagnostics.Assert(this.districtImprovement != null);
		IPersonalityAIHelper personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.populationThresholdForSacrifice = personalityAIHelper.GetRegistryValue<float>(this.Empire, "AI/MajorEmpire/AIEntity_City/AILayer_Population/PopulationThresholdForSacrifice", this.populationThresholdForSacrifice);
		this.approvalThresholdForSacrifice = personalityAIHelper.GetRegistryValue<float>(this.Empire, "AI/MajorEmpire/AIEntity_City/AILayer_Population/ApprovalThresholdForSacrifice", this.approvalThresholdForSacrifice);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI || (base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByHuman && !StaticString.IsNullOrEmpty(this.aiEntityCity.City.AdministrationSpeciality) && this.aiEntityCity.AICityState != null && this.aiEntityCity.AICityState.IsGuiCompliant);
	}

	public override void Release()
	{
		base.Release();
		this.aiEntityCity = null;
		this.assignPopulationOrder = null;
		this.amasCityLayer = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		Diagnostics.Assert(this.amasCityLayer != null);
		AmasCityDataMessage amasCityDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(this.amasCityLayer.AmasCityDataMessageID) as AmasCityDataMessage;
		Diagnostics.Assert(amasCityDataMessage != null);
		this.resourceScore = amasCityDataMessage.PopulationRepartitions;
		Diagnostics.Assert(this.resourceScore != null);
		PreferedPopulationMessage preferedPopulationMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(this.preferedPopulationMessageID) as PreferedPopulationMessage;
		if (preferedPopulationMessage == null)
		{
			preferedPopulationMessage = new PreferedPopulationMessage(this.aiEntityCity.City.GUID);
			this.preferedPopulationMessageID = base.AIEntity.AIPlayer.Blackboard.AddMessage(preferedPopulationMessage);
		}
		preferedPopulationMessage.TimeOut = 1;
		float[] preferedPopulation = new float[this.resourceScore.Length];
		preferedPopulationMessage.PreferedPopulation = preferedPopulation;
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.GeneratePopulationBuyoutMessage();
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.ExecutePopulationBuyout();
		this.ExecutePopulationSacrifice();
		this.assignPopulationOrder = new OrderAssignPopulation(this.Empire.Index, this.aiEntityCity.City.GUID, AILayer_Population.PopulationResource, this.resourceScore);
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_AssignPopulation));
	}

	private void BuyOutPopulation_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		List<EvaluableMessage_PopulationBuyout> list = new List<EvaluableMessage_PopulationBuyout>(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_PopulationBuyout>(BlackboardLayerID.City, (EvaluableMessage_PopulationBuyout message) => message.CityGuid == this.aiEntityCity.City.GUID));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several PopulationBuyout EvaluableMessages for the same city");
		}
		EvaluableMessage_PopulationBuyout evaluableMessage_PopulationBuyout = list[0];
		if (e.Result == PostOrderResponse.Processed)
		{
			evaluableMessage_PopulationBuyout.SetObtained();
		}
		else
		{
			evaluableMessage_PopulationBuyout.SetFailedToObtain();
		}
	}

	private void ExecutePopulationBuyout()
	{
		if (!DepartmentOfTheInterior.CanBuyoutPopulation(this.aiEntityCity.City))
		{
			return;
		}
		List<EvaluableMessage_PopulationBuyout> list = new List<EvaluableMessage_PopulationBuyout>(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_PopulationBuyout>(BlackboardLayerID.City, (EvaluableMessage_PopulationBuyout message) => message.CityGuid == this.aiEntityCity.City.GUID));
		if (list.Count == 0)
		{
			return;
		}
		if (list.Count > 1)
		{
			AILayer.LogWarning("There should not be several PopulationBuyout EvaluableMessages for the same city ({0})", new object[]
			{
				this.aiEntityCity.City
			});
		}
		EvaluableMessage_PopulationBuyout evaluableMessage_PopulationBuyout = list[0];
		if (evaluableMessage_PopulationBuyout.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
		{
			ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
			service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_BuyoutPopulation));
		}
	}

	private void ExecutePopulationSacrifice()
	{
		if (!DepartmentOfTheInterior.CanSacrificePopulation(this.aiEntityCity.City))
		{
			return;
		}
		float propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population);
		if (propertyValue < this.populationThresholdForSacrifice)
		{
			return;
		}
		float propertyValue2 = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityApproval);
		if (propertyValue2 > this.approvalThresholdForSacrifice)
		{
			return;
		}
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_SacrificePopulation));
	}

	private void GeneratePopulationBuyoutMessage()
	{
		if (!DepartmentOfTheInterior.CanBuyoutPopulation(this.aiEntityCity.City))
		{
			return;
		}
		List<EvaluableMessage_PopulationBuyout> list = new List<EvaluableMessage_PopulationBuyout>(this.aiEntityCity.Blackboard.GetMessages<EvaluableMessage_PopulationBuyout>(BlackboardLayerID.City, (EvaluableMessage_PopulationBuyout message) => message.CityGuid == this.aiEntityCity.City.GUID && message.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained && message.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel));
		EvaluableMessage_PopulationBuyout evaluableMessage_PopulationBuyout;
		if (list.Count == 0)
		{
			evaluableMessage_PopulationBuyout = new EvaluableMessage_PopulationBuyout(this.aiEntityCity.City.GUID, 1, AILayer_AccountManager.EconomyAccountName);
			this.aiEntityCity.Blackboard.AddMessage(evaluableMessage_PopulationBuyout);
		}
		else
		{
			evaluableMessage_PopulationBuyout = list[0];
		}
		float num = 0f;
		for (int i = 0; i < AILayer_Population.GainPerPopulation.Length; i++)
		{
			num += this.aiEntityCity.City.GetPropertyValue(AILayer_Population.GainPerPopulation[i]);
		}
		float num2 = 0f;
		for (int j = 0; j < AILayer_Population.Terrain.Length; j++)
		{
			num2 += this.aiEntityCity.City.GetPropertyValue(AILayer_Population.Terrain[j]);
		}
		if (num2 == 0f)
		{
			num2 = 1f;
		}
		float num3 = num / num2;
		float num4 = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityProduction);
		float productionCostWithBonus = DepartmentOfTheTreasury.GetProductionCostWithBonus(this.aiEntityCity.City, this.districtImprovement, DepartmentOfTheTreasury.Resources.Production);
		if (num4 == 0f)
		{
			num4 = 1f;
		}
		float num5 = productionCostWithBonus / num4;
		float num6 = 1f - num5 / 10f;
		if (num6 < 0f)
		{
			num6 = 0f;
		}
		float populationBuyOutCost = DepartmentOfTheTreasury.GetPopulationBuyOutCost(this.aiEntityCity.City);
		int turnGain = Mathf.CeilToInt(num5);
		float num7 = num3 + num6;
		num7 = Mathf.Clamp01(num7);
		evaluableMessage_PopulationBuyout.Refresh(1f, num7, populationBuyOutCost, turnGain);
	}

	private SynchronousJobState SynchronousJob_AssignPopulation()
	{
		if (this.assignPopulationOrder == null)
		{
			return SynchronousJobState.Failure;
		}
		Diagnostics.Assert(this.assignPopulationOrder.PopulationValues != null);
		float num = this.assignPopulationOrder.PopulationValues.Sum();
		float propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Workers);
		if (Math.Abs(num - propertyValue) > 1.401298E-45f)
		{
			AILayer.Log("[AI] Number of assignated workers {0} and real workers {1} doesn't match on city {2}, add the new workers.", new object[]
			{
				num,
				propertyValue,
				this.aiEntityCity.City.GUID
			});
			float num2 = propertyValue - num;
			if (num2 > 0f)
			{
				this.assignPopulationOrder.PopulationValues[0] += num2;
			}
			else
			{
				for (int i = 0; i < this.assignPopulationOrder.PopulationValues.Length; i++)
				{
					float num3 = Mathf.Max(this.assignPopulationOrder.PopulationValues[i] + num2, 0f);
					num2 += this.assignPopulationOrder.PopulationValues[i] - num3;
					this.assignPopulationOrder.PopulationValues[i] = num3;
				}
			}
		}
		Ticket ticket;
		this.Empire.PlayerControllers.AI.PostOrder(this.assignPopulationOrder, out ticket, null);
		return SynchronousJobState.Success;
	}

	private SynchronousJobState SynchronousJob_BuyoutPopulation()
	{
		if (this.aiEntityCity == null || this.aiEntityCity.City == null)
		{
			return SynchronousJobState.Failure;
		}
		if (!DepartmentOfTheInterior.CanBuyoutPopulation(this.aiEntityCity.City))
		{
			return SynchronousJobState.Failure;
		}
		OrderBuyOutPopulation order = new OrderBuyOutPopulation(this.Empire.Index, this.aiEntityCity.City.GUID);
		Ticket ticket;
		this.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.BuyOutPopulation_TicketRaised));
		return SynchronousJobState.Success;
	}

	private SynchronousJobState SynchronousJob_SacrificePopulation()
	{
		if (this.aiEntityCity == null || this.aiEntityCity.City == null)
		{
			return SynchronousJobState.Failure;
		}
		if (!DepartmentOfTheInterior.CanSacrificePopulation(this.aiEntityCity.City))
		{
			return SynchronousJobState.Failure;
		}
		OrderSacrificePopulation order = new OrderSacrificePopulation(this.Empire.Index, this.aiEntityCity.City.GUID);
		this.Empire.PlayerControllers.AI.PostOrder(order);
		Diagnostics.Log("ELCP {0}/{1} sacrificing pops", new object[]
		{
			base.AIEntity.Empire,
			this.aiEntityCity.City.LocalizedName
		});
		return SynchronousJobState.Success;
	}

	public static StaticString[] DistrictResource = new StaticString[]
	{
		SimulationProperties.DistrictFood,
		SimulationProperties.DistrictIndustry,
		SimulationProperties.DistrictScience,
		SimulationProperties.DistrictDust,
		SimulationProperties.DistrictCityPoint
	};

	public static StaticString[] GainPerPopulation = new StaticString[]
	{
		SimulationProperties.BaseFoodPerPopulation,
		SimulationProperties.BaseIndustryPerPopulation,
		SimulationProperties.BaseSciencePerPopulation,
		SimulationProperties.BaseDustPerPopulation,
		SimulationProperties.BaseCityPointPerPopulation
	};

	public static StaticString[] PopulationResource = new StaticString[]
	{
		SimulationProperties.FoodPopulation,
		SimulationProperties.IndustryPopulation,
		SimulationProperties.SciencePopulation,
		SimulationProperties.DustPopulation,
		SimulationProperties.CityPointPopulation
	};

	public static StaticString[] Terrain = new StaticString[]
	{
		SimulationProperties.DistrictFoodNet,
		SimulationProperties.DistrictIndustryNet,
		SimulationProperties.DistrictScienceNet,
		SimulationProperties.DistrictDustNet,
		SimulationProperties.DistrictCityPointNet
	};

	private AIEntity_City aiEntityCity;

	private AILayer_AmasCity amasCityLayer;

	private float approvalThresholdForSacrifice = 40f;

	private OrderAssignPopulation assignPopulationOrder;

	private DepartmentOfIndustry.ConstructibleElement districtImprovement;

	private InterpreterContext interpreterContext;

	private float populationThresholdForSacrifice = 8f;

	private ulong preferedPopulationMessageID;

	private float[] resourceScore;
}
