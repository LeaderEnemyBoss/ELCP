using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

[Diagnostics.TagAttribute("AI")]
public class AILayer_Population : AILayer
{
	static AILayer_Population()
	{
		AILayer_Population.Settler = "Settler";
		AILayer_Population.CitiesWithScienceFocus = new List<GameEntityGUID>[15];
		AILayer_Population.DistrictResource = new StaticString[]
		{
			SimulationProperties.DistrictFood,
			SimulationProperties.DistrictIndustry,
			SimulationProperties.DistrictScience,
			SimulationProperties.DistrictDust,
			SimulationProperties.DistrictCityPoint
		};
		AILayer_Population.GainPerPopulation = new StaticString[]
		{
			SimulationProperties.BaseFoodPerPopulation,
			SimulationProperties.BaseIndustryPerPopulation,
			SimulationProperties.BaseSciencePerPopulation,
			SimulationProperties.BaseDustPerPopulation,
			SimulationProperties.BaseCityPointPerPopulation
		};
		AILayer_Population.PopulationResource = new StaticString[]
		{
			SimulationProperties.FoodPopulation,
			SimulationProperties.IndustryPopulation,
			SimulationProperties.SciencePopulation,
			SimulationProperties.DustPopulation,
			SimulationProperties.CityPointPopulation
		};
		AILayer_Population.Terrain = new StaticString[]
		{
			SimulationProperties.DistrictFoodNet,
			SimulationProperties.DistrictIndustryNet,
			SimulationProperties.DistrictScienceNet,
			SimulationProperties.DistrictDustNet,
			SimulationProperties.DistrictCityPointNet
		};
		AILayer_Population.GlobalPopulationInfos = new AILayer_Population.ELCPGlobalPopulationInfo[15];
		AILayer_Population.CitiesWithDustFocus = new List<GameEntityGUID>[15];
		AILayer_Population.CitiesWithScienceFocus = new List<GameEntityGUID>[15];
		AILayer_Population.EndlessTempleWonderInQueue = "EndlessTempleWonderInQueue";
		AILayer_Population.CityStatusProducingSettler = "CityStatusProducingSettler";
		AILayer_Population.Settler = "Settler";
		AILayer_Population.NonFoodPopPriority = new int[]
		{
			4,
			2,
			1,
			3
		};
	}

	protected global::Empire Empire
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
		if (this.departmentOfIndustry != null)
		{
			this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City).CollectionChanged -= this.ConstructionQueue_CollectionChanged;
			this.departmentOfIndustry = null;
		}
		this.aiEntityCity = null;
		this.assignPopulationOrder = null;
		this.amasCityLayer = null;
		this.departmentOfTheInterior = null;
		this.departmentOfForeignAffairs = null;
		this.departmentOfTheTreasury = null;
		this.game = null;
		this.aILayer_Victory = null;
		AILayer_Population.CitiesWithDustFocus[this.Empire.Index].Clear();
		AILayer_Population.CitiesWithScienceFocus[this.Empire.Index].Clear();
		base.Release();
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.CreateLocalNeeds_ELCPGlobalDustNeed();
		this.CreateLocalNeeds_ELCPGlobalScienceNeed();
		Diagnostics.Assert(this.amasCityLayer != null);
		AmasCityDataMessage amasCityDataMessage = base.AIEntity.AIPlayer.Blackboard.GetMessage(this.amasCityLayer.AmasCityDataMessageID) as AmasCityDataMessage;
		Diagnostics.Assert(amasCityDataMessage != null);
		this.resourceScore = amasCityDataMessage.PopulationRepartitions;
		if (this.aiEntityCity.City.SimulationObject.Tags.Contains(AILayer_Population.CityStatusProducingSettler))
		{
			this.resourceScore[1] += this.resourceScore[0];
			this.resourceScore[0] = 0f;
		}
		if (AILayer_Population.CitiesWithDustFocus[this.Empire.Index].Contains(this.aiEntityCity.City.GUID))
		{
			string format = "ELCP {0} city {1} selected for victory dust focus, original resource scores {2}";
			object[] array = new object[3];
			array[0] = this.Empire;
			array[1] = this.aiEntityCity.City.LocalizedName;
			array[2] = string.Join(", ", this.resourceScore.Select(delegate(float x)
			{
				float num4 = x;
				return num4.ToString();
			}).ToArray<string>());
			Diagnostics.Log(format, array);
			float num = 0f;
			for (int i = 1; i < 5; i++)
			{
				num += this.resourceScore[i];
				this.resourceScore[i] = 0f;
			}
			this.resourceScore[3] = num;
		}
		if (AILayer_Population.CitiesWithScienceFocus[this.Empire.Index].Contains(this.aiEntityCity.City.GUID))
		{
			string format2 = "ELCP {0} city {1} selected for victory science focus, original resource scores {2}";
			object[] array2 = new object[3];
			array2[0] = this.Empire;
			array2[1] = this.aiEntityCity.City.LocalizedName;
			array2[2] = string.Join(", ", this.resourceScore.Select(delegate(float x)
			{
				float num4 = x;
				return num4.ToString();
			}).ToArray<string>());
			Diagnostics.Log(format2, array2);
			int[] array3 = new int[]
			{
				1,
				2,
				4
			};
			float num2 = 0f;
			foreach (int num3 in array3)
			{
				num2 += this.resourceScore[num3];
				this.resourceScore[num3] = 0f;
			}
			this.resourceScore[2] = num2;
		}
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
		this.assignedPopulationThisTurn = false;
		AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_AssignPopulation));
		AILayer_Population.CitiesWithDustFocus[this.Empire.Index].Clear();
		AILayer_Population.CitiesWithScienceFocus[this.Empire.Index].Clear();
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
		Diagnostics.Log("ELCP {0}/{1} ExecutePopulationBuyout {2}", new object[]
		{
			this.Empire,
			this.aiEntityCity.City,
			evaluableMessage_PopulationBuyout.EvaluationState
		});
		if (evaluableMessage_PopulationBuyout.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
		{
			AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_BuyoutPopulation));
		}
	}

	private void ExecutePopulationSacrifice()
	{
		if (!DepartmentOfTheInterior.CanSacrificePopulation(this.aiEntityCity.City))
		{
			return;
		}
		if (this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population) < this.populationThresholdForSacrifice)
		{
			return;
		}
		if (this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityApproval) > this.approvalThresholdForSacrifice)
		{
			return;
		}
		Booster[] activeBoosters = this.Empire.GetAgency<DepartmentOfPlanificationAndDevelopment>().GetActiveBoosters();
		for (int i = 0; i < activeBoosters.Length; i++)
		{
			if (activeBoosters[i].TargetGUID == this.aiEntityCity.City.GUID && activeBoosters[i].BoosterDefinition.Name == "BoosterSacrificePopulation" && activeBoosters[i].RemainingTime > 1)
			{
				return;
			}
		}
		AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_SacrificePopulation));
	}

	private void GeneratePopulationBuyoutMessage()
	{
		if (!DepartmentOfTheInterior.CanBuyoutPopulation(this.aiEntityCity.City))
		{
			return;
		}
		this.GeneratePopulationBuyoutMessage_ELCPGlobalPopulationInfo();
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
		float num2 = num / AILayer_Population.GlobalPopulationInfos[this.Empire.Index].bestGainPerPop * 0.5f;
		float propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population);
		if (propertyValue == AILayer_Population.GlobalPopulationInfos[this.Empire.Index].lowestPopulation)
		{
			num2 = AILayer.Boost(num2, 0.2f);
		}
		float populationBuyOutCost = DepartmentOfTheTreasury.GetPopulationBuyOutCost(this.aiEntityCity.City);
		float num3;
		if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num3, false))
		{
			num3 = 1f;
		}
		float num4 = (num3 - populationBuyOutCost) / num3 / 0.8f;
		if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			num4 -= 0.05f;
		}
		num2 = AILayer.Boost(num2, num4);
		ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City);
		if (propertyValue > 1f)
		{
			for (int j = constructionQueue.Length - 1; j >= 0; j--)
			{
				if (constructionQueue.PeekAt(j).ConstructibleElementName.ToString().Contains("Settler"))
				{
					num2 = AILayer.Boost(num2, -1f);
					break;
				}
			}
		}
		Diagnostics.Log("ELCP {0}/{1} GeneratePopulationBuyoutMessage score: {2}, cost: {3}", new object[]
		{
			this.Empire,
			this.aiEntityCity.City.LocalizedName,
			num2,
			populationBuyOutCost
		});
		evaluableMessage_PopulationBuyout.Refresh(1f, num2, populationBuyOutCost, int.MaxValue);
	}

	private SynchronousJobState SynchronousJob_AssignPopulation()
	{
		if (this.assignPopulationOrder == null)
		{
			this.assignedPopulationThisTurn = true;
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
		this.Empire.PlayerControllers.AI.PostOrder(this.assignPopulationOrder, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderAssignPopulation_TicketRaised));
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

	private void GeneratePopulationBuyoutMessage_ELCPGlobalPopulationInfo()
	{
		if (AILayer_Population.GlobalPopulationInfos[this.Empire.Index] == null)
		{
			AILayer_Population.GlobalPopulationInfos[this.Empire.Index] = new AILayer_Population.ELCPGlobalPopulationInfo();
		}
		AILayer_Population.ELCPGlobalPopulationInfo elcpglobalPopulationInfo = AILayer_Population.GlobalPopulationInfos[this.Empire.Index];
		if (elcpglobalPopulationInfo.lastUpdateTurn == this.game.Turn)
		{
			return;
		}
		elcpglobalPopulationInfo.lastUpdateTurn = this.game.Turn;
		elcpglobalPopulationInfo.lowestPopulation = float.MaxValue;
		elcpglobalPopulationInfo.bestGainPerPop = float.MinValue;
		foreach (City city in this.departmentOfTheInterior.Cities)
		{
			if (city != null && !city.IsInfected && city.SimulationObject != null && !city.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
			{
				float propertyValue = city.GetPropertyValue(SimulationProperties.Population);
				if (propertyValue < elcpglobalPopulationInfo.lowestPopulation)
				{
					elcpglobalPopulationInfo.lowestPopulation = propertyValue;
				}
				float num = 0f;
				for (int i = 0; i < AILayer_Population.GainPerPopulation.Length; i++)
				{
					num += city.GetPropertyValue(AILayer_Population.GainPerPopulation[i]);
				}
				if (num > elcpglobalPopulationInfo.bestGainPerPop)
				{
					elcpglobalPopulationInfo.bestGainPerPop = num;
				}
			}
		}
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfForeignAffairs = this.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.departmentOfIndustry = this.Empire.GetAgency<DepartmentOfIndustry>();
		this.departmentOfTheTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City).CollectionChanged += this.ConstructionQueue_CollectionChanged;
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.AIEntity.Empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				this.aILayer_Victory = entity.GetLayer<AILayer_Victory>();
			}
		}
		this.sciencePhobic = this.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1);
		AILayer_Population.CitiesWithDustFocus[this.Empire.Index] = new List<GameEntityGUID>();
		AILayer_Population.CitiesWithScienceFocus[this.Empire.Index] = new List<GameEntityGUID>();
		yield break;
	}

	private void CreateLocalNeeds_ELCPGlobalDustNeed()
	{
		if ((this.aILayer_Victory.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Economy || (this.aILayer_Victory.CurrentFocusEnum == AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered && this.sciencePhobic)) && AILayer_Population.CitiesWithDustFocus[this.Empire.Index].Count == 0)
		{
			List<City> list = new List<City>();
			City city = null;
			foreach (City city2 in this.departmentOfTheInterior.Cities)
			{
				if (city2 != null && !city2.IsInfected && city2.SimulationObject != null && !city2.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
				{
					if (city == null)
					{
						using (IEnumerator<SimulationDescriptor> enumerator2 = ((IDescriptorEffectProvider)city2).GetDescriptors().GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								if (enumerator2.Current.Name == AILayer_Population.EndlessTempleWonderInQueue)
								{
									city = city2;
								}
							}
						}
					}
					if (city != city2)
					{
						list.Add(city2);
					}
				}
			}
			list = (from o in list
			orderby o.GetPropertyValue(SimulationProperties.BaseDustPerPopulation) descending
			select o).ToList<City>();
			if (city != null)
			{
				list.Add(city);
			}
			int count = (int)Mathf.Ceil((float)list.Count * 0.3f);
			list = list.Take(count).ToList<City>();
			AILayer_Population.CitiesWithDustFocus[this.Empire.Index] = (from guid in list
			select guid.GUID).ToList<GameEntityGUID>();
		}
	}

	private void CreateLocalNeeds_ELCPGlobalScienceNeed()
	{
		if (this.aILayer_Victory.CurrentFocusEnum == AILayer_Victory.VictoryFocus.MostTechnologiesDiscovered && !this.sciencePhobic && AILayer_Population.CitiesWithScienceFocus[this.Empire.Index].Count == 0)
		{
			List<City> list = new List<City>();
			City city = null;
			foreach (City city2 in this.departmentOfTheInterior.Cities)
			{
				if (city2 != null && !city2.IsInfected && city2.SimulationObject != null && !city2.SimulationObject.Tags.Contains(City.TagCityStatusRazed))
				{
					if (city == null)
					{
						using (IEnumerator<SimulationDescriptor> enumerator2 = ((IDescriptorEffectProvider)city2).GetDescriptors().GetEnumerator())
						{
							while (enumerator2.MoveNext())
							{
								if (enumerator2.Current.Name == AILayer_Population.EndlessTempleWonderInQueue)
								{
									city = city2;
								}
							}
						}
					}
					if (city != city2)
					{
						list.Add(city2);
					}
				}
			}
			list = (from o in list
			orderby o.GetPropertyValue(SimulationProperties.BaseSciencePerPopulation) descending
			select o).ToList<City>();
			if (city != null)
			{
				list.Add(city);
			}
			int count = (int)Mathf.Ceil((float)list.Count * 0.4f);
			list = list.Take(count).ToList<City>();
			AILayer_Population.CitiesWithScienceFocus[this.Empire.Index] = (from guid in list
			select guid.GUID).ToList<GameEntityGUID>();
		}
	}

	private void ConstructionQueue_CollectionChanged(object sender, CollectionChangeEventArgs e)
	{
		ConstructionQueue constructionQueue = this.departmentOfIndustry.GetConstructionQueue(this.aiEntityCity.City);
		if (constructionQueue != null && constructionQueue.Length > 0)
		{
			UnitDesign unitDesign = constructionQueue.Peek().ConstructibleElement as UnitDesign;
			if (unitDesign != null && this.IsActive() && unitDesign.UnitBodyDefinition != null && unitDesign.UnitBodyDefinition.Tags.Contains(AILayer_Population.Settler) && this.resourceScore[0] > 0f)
			{
				this.resourceScore[1] += this.resourceScore[0];
				this.resourceScore[0] = 0f;
				if (this.assignedPopulationThisTurn)
				{
					this.assignedPopulationThisTurn = false;
					AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_AssignPopulation));
				}
			}
		}
	}

	private void OrderAssignPopulation_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		for (int i = 0; i < AILayer_Population.PopulationResource.Length; i++)
		{
			this.resourceScore[i] = this.aiEntityCity.City.GetPropertyValue(AILayer_Population.PopulationResource[i]);
		}
		float propertyValue = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.NetCityGrowth);
		if (propertyValue < 0f)
		{
			float propertyValue2 = this.aiEntityCity.City.GetPropertyValue(SimulationProperties.Population);
			float num = DepartmentOfTheInterior.ComputeGrowthLimit(this.Empire.SimulationObject, propertyValue2);
			if (this.aiEntityCity.City.GetPropertyValue(SimulationProperties.CityGrowthStock) + propertyValue < num)
			{
				foreach (int num2 in AILayer_Population.NonFoodPopPriority)
				{
					if (this.resourceScore[num2] >= 1f)
					{
						this.resourceScore[num2] -= 1f;
						this.resourceScore[0] += 1f;
						this.assignedPopulationThisTurn = false;
						AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>().RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_AssignPopulation));
						return;
					}
				}
			}
		}
		this.assignedPopulationThisTurn = true;
	}

	public static StaticString[] DistrictResource;

	public static StaticString[] GainPerPopulation;

	public static StaticString[] PopulationResource;

	public static StaticString[] Terrain;

	private AIEntity_City aiEntityCity;

	private AILayer_AmasCity amasCityLayer;

	private float approvalThresholdForSacrifice = 40f;

	private OrderAssignPopulation assignPopulationOrder;

	private DepartmentOfIndustry.ConstructibleElement districtImprovement;

	private InterpreterContext interpreterContext;

	private float populationThresholdForSacrifice = 8f;

	private ulong preferedPopulationMessageID;

	private float[] resourceScore;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfIndustry departmentOfIndustry;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private global::Game game;

	private static AILayer_Population.ELCPGlobalPopulationInfo[] GlobalPopulationInfos;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private bool sciencePhobic;

	private static List<GameEntityGUID>[] CitiesWithDustFocus;

	private AILayer_Victory aILayer_Victory;

	public static StaticString EndlessTempleWonderInQueue;

	private static List<GameEntityGUID>[] CitiesWithScienceFocus;

	private bool assignedPopulationThisTurn;

	private static StaticString CityStatusProducingSettler;

	public static StaticString Settler;

	private static int[] NonFoodPopPriority = new int[]
	{
		4,
		2,
		1,
		3
	};

	private class ELCPGlobalPopulationInfo
	{
		public ELCPGlobalPopulationInfo()
		{
			this.lastUpdateTurn = -1;
		}

		public int lastUpdateTurn;

		public float lowestPopulation;

		public float bestGainPerPop;
	}
}
