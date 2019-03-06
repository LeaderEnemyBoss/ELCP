using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Decision.Diagnostics;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Xml;
using UnityEngine;

public class AILayer_UnitDesigner : AILayer, ISimulationAIEvaluationHelper<AIItemData>, IAIEvaluationHelper<AIItemData, InterpreterContext>
{
	public AILayer_UnitDesigner()
	{
		this.afterBattleDataByBody = new List<AfterBattleData>();
		this.availableItemData = new List<AIItemData>();
		this.bestTierByResourceName = new Dictionary<StaticString, string>();
		this.costModifier = 0.5f;
		this.decisionResult = new List<DecisionResult>();
		this.heroDesignToImprove = new List<UnitDesign>();
		this.unitAssignationParameterModifier = new List<AIParameter.AIModifier>();
		this.scoreLimit = 0.05f;
		this.unitDesignToImprove = new List<UnitDesign>();
		this.newUnitDesignScoreModifier = 0.8f;
		this.unitSlayerModifier = new List<AIParameter.AIModifier>();
		this.SetItems = new List<string>();
		this.ExpectedResourceUsage = new Dictionary<string, float>
		{
			{
				"Strategic1",
				0f
			},
			{
				"Strategic2",
				0f
			},
			{
				"Strategic3",
				0f
			},
			{
				"Strategic4",
				0f
			},
			{
				"Strategic5",
				0f
			},
			{
				"Strategic6",
				0f
			}
		};
	}

	public void Register(StaticString unitBodyName, StaticString afterBattleSimulation, float value)
	{
		for (int i = 0; i < this.afterBattleDataByBody.Count; i++)
		{
			if (this.afterBattleDataByBody[i].UnitBodyName == unitBodyName)
			{
				this.afterBattleDataByBody[i].RegisterEncounterData(afterBattleSimulation, value);
			}
		}
	}

	private void AILayer_Encounter_EncounterRoundUpdate(object sender, RoundUpdateEventArgs eventArgs)
	{
		for (int i = 0; i < eventArgs.RoundReport.RoundContenderReports.Count; i++)
		{
			if (eventArgs.Encounter.Contenders[i].Empire == base.AIEntity.Empire)
			{
				Contender contender = eventArgs.Encounter.Contenders[i];
				if (contender.IsTakingPartInBattle)
				{
					RoundContenderReport roundContenderReport = eventArgs.RoundReport.RoundContenderReports[i];
					for (int j = 0; j < roundContenderReport.RoundUnitReports.Count; j++)
					{
						RoundUnitReport roundUnitReport = roundContenderReport.RoundUnitReports[j];
						EncounterUnit encounterUnitByGUID = contender.Encounter.GetEncounterUnitByGUID(roundUnitReport.UnitGUID);
						if (encounterUnitByGUID.WorldPosition.IsValid)
						{
							int num = 0;
							int num2 = 0;
							for (int k = 0; k < roundUnitReport.RoundUnitStateReports.Count; k++)
							{
								for (int l = 0; l < roundUnitReport.RoundUnitStateReports[k].Instructions.Count; l++)
								{
									if (roundUnitReport.RoundUnitStateReports[k].Instructions[l] is UnitActionAttackInstruction)
									{
										num2++;
									}
									UnitActionDefendInstruction unitActionDefendInstruction = roundUnitReport.RoundUnitStateReports[k].Instructions[l] as UnitActionDefendInstruction;
									if (unitActionDefendInstruction != null)
									{
										num++;
									}
								}
							}
							this.Register(encounterUnitByGUID.Unit.UnitDesign.UnitBodyDefinition.Name, "Attack", (float)num2);
							this.Register(encounterUnitByGUID.Unit.UnitDesign.UnitBodyDefinition.Name, "Defense", (float)num);
							for (int m = 0; m < roundUnitReport.CriticalInstructions.Count; m++)
							{
								this.AnalyzeInstruction(contender, roundUnitReport.CriticalInstructions[m]);
							}
						}
					}
				}
			}
		}
	}

	private void AnalyzeInstruction(Contender contender, IUnitReportInstruction reportInstruction)
	{
		if (!(reportInstruction is IReportSimulationInstruction))
		{
			BattleEffectUpdateInstruction battleEffectUpdateInstruction = reportInstruction as BattleEffectUpdateInstruction;
			if (battleEffectUpdateInstruction != null)
			{
				for (int i = 0; i < battleEffectUpdateInstruction.ReportInstructions.Count; i++)
				{
					this.AnalyzeInstruction(contender, battleEffectUpdateInstruction.ReportInstructions[i]);
				}
			}
			return;
		}
		EncounterUnit encounterUnitByGUID = contender.Encounter.GetEncounterUnitByGUID(reportInstruction.UnitGUID);
		if (encounterUnitByGUID == null)
		{
			return;
		}
		SimulationPropertyChangeInstruction simulationPropertyChangeInstruction = reportInstruction as SimulationPropertyChangeInstruction;
		if (simulationPropertyChangeInstruction != null)
		{
			float propertyValue = encounterUnitByGUID.UnitDuplicatedSimulationObject.GetPropertyValue(simulationPropertyChangeInstruction.PropertyName);
			float value = simulationPropertyChangeInstruction.Value;
			this.UpdateSimulationProperty(encounterUnitByGUID, simulationPropertyChangeInstruction.PropertyName, propertyValue, value);
		}
	}

	private float ComputeEmpireImportance(global::Empire otherEmpire)
	{
		float result = 0f;
		if (otherEmpire is MinorEmpire)
		{
			result = 0.05f;
		}
		else
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(otherEmpire);
			if (diplomaticRelation != null && diplomaticRelation.State != null)
			{
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar)
				{
					result = 0.2f;
				}
				else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
				{
					result = 0.8f;
				}
			}
		}
		return result;
	}

	private void RunBattleAnalzer()
	{
		int turn = this.endTurnService.Turn;
		foreach (AfterBattleData afterBattleData in this.afterBattleDataByBody)
		{
			afterBattleData.TurnUpdate(turn);
			this.afterBattleAnalyzer.Analyze(afterBattleData);
		}
	}

	private StaticString TransformHitInfoToEncounterData(float value, string typeOfHit)
	{
		if (value == 0f)
		{
			return "Fail" + typeOfHit;
		}
		if (value == 1f)
		{
			return "Fumble" + typeOfHit;
		}
		if (value == 3f)
		{
			return "Critical" + typeOfHit;
		}
		return "Normal" + typeOfHit;
	}

	private void UpdateSimulationProperty(EncounterUnit encounterUnit, StaticString propertyName, float current, float finalValue)
	{
		StaticString name = encounterUnit.Unit.UnitDesign.UnitBodyDefinition.Name;
		if (propertyName == SimulationProperties.AttackingHitInfo)
		{
			propertyName = this.TransformHitInfoToEncounterData(finalValue, "Defense");
			this.Register(name, propertyName, 1f);
			this.Register(name, "NumberOfParade", 1f);
		}
		if (propertyName == SimulationProperties.HitInfo)
		{
			propertyName = this.TransformHitInfoToEncounterData(finalValue, "Attack");
			this.Register(name, propertyName, 1f);
			this.Register(name, "NumberOfHit", 1f);
		}
		if (propertyName == SimulationProperties.Health)
		{
			this.Register(name, "DamageTaken", current - finalValue);
		}
	}

	private void UpdateUnitTypeBasedModifier()
	{
		float num = 0f;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			if (i != base.AIEntity.Empire.Index)
			{
				global::Empire otherEmpire = this.game.Empires[i];
				float num2 = this.ComputeEmpireImportance(otherEmpire);
				AIEmpireData aiempireData;
				if (this.empireDataHelper.TryGet(i, out aiempireData))
				{
					num += (float)aiempireData.MilitaryStandardUnitCount * num2;
					KeyValuePair<StaticString, int> kvp;
					foreach (KeyValuePair<StaticString, int> kvp2 in aiempireData.CountPerUnitType)
					{
						kvp = kvp2;
						AIParameter.AIModifier aimodifier = this.unitSlayerModifier.Find((AIParameter.AIModifier match) => match.Name == kvp.Key);
						if (aimodifier == null)
						{
							aimodifier = new AIParameter.AIModifier(kvp.Key, 0f);
							this.unitSlayerModifier.Add(aimodifier);
						}
						float num3 = (float)kvp.Value * num2;
						aimodifier.Value += num3;
					}
				}
			}
		}
		if (num > 0f)
		{
			for (int j = 0; j < this.unitSlayerModifier.Count; j++)
			{
				this.unitSlayerModifier[j].Value /= num;
			}
		}
	}

	public IEnumerable<IAIParameterConverter<InterpreterContext>> GetAIParameterConverters(StaticString aiParameterName)
	{
		Diagnostics.Assert(this.aiParameterConverterDatabase != null);
		AIParameterConverter aiParameterConverter;
		if (!this.aiParameterConverterDatabase.TryGetValue(aiParameterName, out aiParameterConverter))
		{
			yield break;
		}
		Diagnostics.Assert(aiParameterConverter != null);
		if (aiParameterConverter.ToAIParameters == null)
		{
			yield break;
		}
		for (int index = 0; index < aiParameterConverter.ToAIParameters.Length; index++)
		{
			yield return aiParameterConverter.ToAIParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(AIItemData element)
	{
		if (element.AIParameters == null)
		{
			Diagnostics.LogWarning("Invalid null ai parameters. Please check the data for item: {0}.", new object[]
			{
				element.ItemDefinition.Name
			});
			yield break;
		}
		for (int index = 0; index < element.AIParameters.Length; index++)
		{
			yield return element.AIParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(AIItemData element)
	{
		yield break;
	}

	private float DecisionParameterContextModifier(AIItemData aiEvaluableElement, StaticString aiParameterName)
	{
		for (int i = 0; i < this.unitSlayerModifier.Count; i++)
		{
			if (this.unitSlayerModifier[i].Name == aiParameterName)
			{
				return this.unitSlayerModifier[i].Value;
			}
		}
		for (int j = 0; j < this.unitAssignationParameterModifier.Count; j++)
		{
			if (this.unitAssignationParameterModifier[j].Name == aiParameterName)
			{
				return this.unitAssignationParameterModifier[j].Value;
			}
		}
		if (this.currentAfterBattleData != null)
		{
			return this.currentAfterBattleData.GetModifierValue(aiParameterName);
		}
		return 0f;
	}

	private void DecisionParameterContextNormalization(StaticString aiParameterName, out float minimumValue, out float maximumValue)
	{
		minimumValue = 0f;
		maximumValue = 1f;
		AIParameter.AIModifier[] array = this.modifierMaximalValue;
		if (this.isCurrentDesignSeafaring)
		{
			array = this.modifierNavalMaximalValue;
		}
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].Name == aiParameterName)
			{
				maximumValue = array[i].Value;
				break;
			}
		}
		if (this.modifierBoost != null)
		{
			for (int j = 0; j < this.modifierBoost.Length; j++)
			{
				if (this.modifierBoost[j].Name == aiParameterName)
				{
					maximumValue = AILayer.Boost(maximumValue, this.modifierBoost[j].Value);
					break;
				}
			}
		}
	}

	private bool HasSlotType(UnitEquipmentSet unitEquipmentSet, StaticString slotName)
	{
		for (int i = 0; i < unitEquipmentSet.Slots.Length; i++)
		{
			if (unitEquipmentSet.Slots[i].SlotType == slotName)
			{
				return true;
			}
		}
		return false;
	}

	private float ItemScoreTransferFunction(AIItemData element, float currentScore)
	{
		float num;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false);
		if (!element.IsNaval)
		{
			if (element.ResourceName == "Iron" && (this.bestTierByResourceName[element.ResourceName] != element.Tier || (base.AIEntity.Empire.GetAgency<DepartmentOfScience>().CurrentTechnologyEraNumber > 3 && num > 600f && !element.ToString().Contains("Ring") && !element.ToString().Contains("Talisman"))))
			{
				currentScore = -2f;
				return currentScore;
			}
			float registryValue = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/TierModifier/{1}", AILayer_UnitDesigner.RegistryPath, element.Tier), 1f);
			currentScore *= registryValue;
		}
		if (element.ToString() == "ItemLegsLavaWalker")
		{
			if (!Array.Exists<UnitAbilityReference>(this.CurrentUnitDesign.UnitBodyDefinition.UnitAbilities, (UnitAbilityReference refe) => refe.Name == "UnitAbilityFlamewalker" || refe.Name == "UnitAbilityInnerFire"))
			{
				currentScore *= 3f;
			}
		}
		if (element.ToString() == "ItemTalismanIronTier3")
		{
			if (this.HeroIsSlowpoke)
			{
				currentScore *= 3f;
			}
			else
			{
				float attributeValue = this.CurrentUnitDesign.UnitBodyDefinition.GetAttributeValue(SimulationProperties.MaximumMovementOnLand);
				currentScore *= 1f + Mathf.Max(0f, (7f - attributeValue) * 0.4f);
			}
		}
		if (this.boostVictoryItem && element.ToString() == "ItemTalismanVictoryQuest2")
		{
			currentScore = 20f;
		}
		float num2 = 0f;
		bool flag = this.IgnoreCostForCurrentDesign;
		AIUnitDesignData aiunitDesignData;
		if (!flag && this.Hero != null && this.HeroDesign && this.unitDesignRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, this.Hero.UnitDesign.Model, out aiunitDesignData) && aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot.Contains(element.ToString()))
		{
			flag = true;
		}
		if (!flag)
		{
			for (int i = 0; i < element.ItemDefinition.Costs.Length; i++)
			{
				bool flag2 = false;
				for (int j = 0; j < this.resourcePolicy.Count; j++)
				{
					if (this.resourcePolicy[j].Name == element.ItemDefinition.Costs[i].ResourceName)
					{
						float num3;
						float num4;
						if (element.ItemDefinition.Costs[i].ResourceName == "EmpireMoney" && num > 200f)
						{
							num3 = this.resourcePolicy[j].Value + num / 25f;
						}
						else if (((this.HeroDesign && !this.Governorhero && !this.Spyhero) || ((this.Governorhero || this.Spyhero) && element.ItemDefinition.Slots[0].SlotType == "Accessory")) && element.ItemDefinition.Costs[i].ResourceName.ToString().Contains("Strategic") && this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, element.ItemDefinition.Costs[i].ResourceName, out num4, false))
						{
							num3 = this.resourcePolicy[j].Value;
							if (num4 > 4f * element.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) && num4 / 2f > num3)
							{
								num3 = num4 / 2f;
							}
						}
						else
						{
							if (this.Hero == null && !element.IsNaval && !this.CurrentUnitDesign.Tags.Contains(DownloadableContent9.TagSolitary) && element.ItemDefinition.Costs[i].ResourceName.ToString().Contains("Strategic"))
							{
								if (this.ExpectedResourceUsage[element.ItemDefinition.Costs[i].ResourceName.ToString()] <= 0f)
								{
									return -2f;
								}
								if (this.OriginalNonHeroDesign)
								{
									Dictionary<string, float> expectedResourceUsage = this.ExpectedResourceUsage;
									string text = element.ItemDefinition.Costs[i].ResourceName.ToString();
									Dictionary<string, float> dictionary = expectedResourceUsage;
									Dictionary<string, float> dictionary2 = dictionary;
									string key = text;
									dictionary2[key] -= element.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) * this.ResourceLimitMultiplier;
								}
							}
							num3 = this.resourcePolicy[j].Value;
						}
						num2 = Mathf.Min(1f, Mathf.Max(num2, element.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) / num3));
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					num2 = 2f;
					break;
				}
			}
		}
		currentScore -= num2 * this.costModifier;
		bool flag3 = false;
		bool flag4 = false;
		bool flag5 = false;
		if (this.Hero != null && this.HeroDesign)
		{
			for (int k = 0; k < element.AIParameters.Length; k++)
			{
				if (!flag3 && (element.AIParameters[k].Name.ToString().Contains("Governor") || element.AIParameters[k].Name.ToString().Contains("AIItem")))
				{
					flag3 = (element.AIParameters[k].GetValue(new InterpreterContext(this.Hero.SimulationObject)) > 0f);
				}
				if (!flag4 && (element.AIParameters[k].Name.ToString().Contains("AIEquipment") || element.AIParameters[k].Name.ToString().Contains("AIUnit") || element.AIParameters[k].Name.ToString().Contains("Army")))
				{
					flag4 = (element.AIParameters[k].GetValue(new InterpreterContext(this.Hero.SimulationObject)) > 0f);
				}
				if (!flag5 && element.AIParameters[k].Name.ToString().Contains("Spy"))
				{
					flag5 = (element.AIParameters[k].GetValue(new InterpreterContext(this.Hero.SimulationObject)) > 0f);
				}
			}
			if (this.Governorhero)
			{
				if (flag3)
				{
					currentScore *= 3f;
				}
				else if (element.ItemDefinition.Slots[0].SlotType == "Accessory" && currentScore > 0.1f)
				{
					currentScore = 0.1f;
				}
				else
				{
					currentScore *= 0.5f;
				}
			}
			if (!this.Governorhero && !this.Spyhero && currentScore > 0f && !flag4)
			{
				currentScore = 0f;
			}
			if (this.Spyhero)
			{
				if (flag5)
				{
					currentScore *= 10f;
				}
				else if (element.ItemDefinition.Slots[0].SlotType == "Accessory")
				{
					currentScore = 0f;
				}
				else
				{
					currentScore *= 0.5f;
				}
			}
		}
		if (currentScore >= 0f && element.ItemDefinition.Slots[0].SlotType == "Accessory" && this.DontNeedCapacity(element.ItemDefinition))
		{
			currentScore = -2f;
		}
		if (this.Hero != null && this.Hero.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyDualWield, -1) && element.ItemDefinition.Slots[0].SlotTypes.Length == 1)
		{
			currentScore *= 1.5f;
		}
		return currentScore;
	}

	public override void ReadXml(XmlReader reader)
	{
		base.ReadXml(reader);
		if (reader.IsStartElement("AfterBattleDataList"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("AfterBattleDataList");
			for (int i = 0; i < attribute; i++)
			{
				string attribute2 = reader.GetAttribute("Name");
				AfterBattleData afterBattleData = this.FindAfterBattleData(attribute2);
				if (afterBattleData == null)
				{
					reader.Skip();
				}
				else
				{
					reader.ReadElementSerializable<AfterBattleData>("AfterBattleData", ref afterBattleData);
				}
			}
			reader.ReadEndElement();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		base.WriteXml(writer);
		writer.WriteStartElement("AfterBattleDataList");
		writer.WriteAttributeString<int>("Count", this.afterBattleDataByBody.Count);
		for (int i = 0; i < this.afterBattleDataByBody.Count; i++)
		{
			AfterBattleData afterBattleData = this.afterBattleDataByBody[i];
			writer.WriteElementSerializable<AfterBattleData>("AfterBattleData", ref afterBattleData);
		}
		writer.WriteEndElement();
	}

	private AfterBattleData FindAfterBattleData(StaticString afterBattleDataName)
	{
		for (int i = 0; i < this.afterBattleDataByBody.Count; i++)
		{
			if (this.afterBattleDataByBody[i].UnitBodyName == afterBattleDataName)
			{
				return this.afterBattleDataByBody[i];
			}
		}
		return null;
	}

	public List<AfterBattleData> AfterBattleDataByUnitBody
	{
		get
		{
			return this.afterBattleDataByBody;
		}
	}

	public override IEnumerator Initialize(AIEntity ai)
	{
		yield return base.Initialize(ai);
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		Diagnostics.Assert(this.departmentOfDefense != null);
		this.departmentOfDefense.AvailableUnitBodyChanged += this.DepartmentOfDefense_AvailableUnitBodyChanged;
		this.departmentOfEducation = base.AIEntity.Empire.GetAgency<DepartmentOfEducation>();
		Diagnostics.Assert(this.departmentOfEducation != null);
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.synchronousJobRepositoryHelper = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		Diagnostics.Assert(this.synchronousJobRepositoryHelper != null);
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.game = (gameService.Game as global::Game);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_UnitDesigner_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"AILayer_ResourceManager_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_UnitDesigner_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		Diagnostics.Assert(this.game != null);
		this.itemDataRepository = AIScheduler.Services.GetService<IAIItemDataRepository>();
		this.personalityHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.unitDesignRepository = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
		this.empireDataHelper = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		this.itemDataDecisionMaker = new SimulationDecisionMaker<AIItemData>(this, null);
		this.itemDataDecisionMaker.ParameterContextModifierDelegate = new Func<AIItemData, StaticString, float>(this.DecisionParameterContextModifier);
		this.itemDataDecisionMaker.ParameterContextNormalizationDelegate = new DecisionMaker<AIItemData, InterpreterContext>.GetNormalizationRangeDelegate(this.DecisionParameterContextNormalization);
		this.itemDataDecisionMaker.ScoreTransferFunctionDelegate = new Func<AIItemData, float, float>(this.ItemScoreTransferFunction);
		this.scoreLimit = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_UnitDesigner.RegistryPath, "ItemScoreLimit"), this.scoreLimit);
		this.costModifier = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_UnitDesigner.RegistryPath, "ItemCostRatioBoost"), this.costModifier);
		this.newUnitDesignScoreModifier = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", AILayer_UnitDesigner.RegistryPath, "NewUnitDesignScoreModifier"), this.newUnitDesignScoreModifier);
		this.afterBattleAnalyzer = new AfterBattleAnalyzer_Default();
		this.afterBattleDatabase = Databases.GetDatabase<AfterBattleDefinition>(false);
		this.aiLayerEncounter = base.AIEntity.GetLayer<AILayer_Encounter>();
		this.aiLayerEncounter.EncounterRoundUpdate += this.AILayer_Encounter_EncounterRoundUpdate;
		AfterBattleDefinition battleDefinition;
		if (this.empireModifiers == null && this.afterBattleDatabase.TryGetValue("EmpireDefault", out battleDefinition))
		{
			this.empireModifiers = new AfterBattleData();
			this.empireModifiers.Initialize(battleDefinition, 5);
		}
		if (this.empireModifiers == null)
		{
			yield break;
		}
		if (this.afterBattleDataByBody.Count == 0)
		{
			foreach (UnitBodyDefinition unitBody in this.departmentOfDefense.UnitDesignDatabase.GetAvailableUnitBodyDefinitionsAsEnumerable())
			{
				AfterBattleDefinition battleDefinition2;
				if (this.afterBattleDatabase.TryGetValue(unitBody.Name, out battleDefinition2))
				{
					AfterBattleData unitBodyData = new AfterBattleData();
					unitBodyData.Initialize(battleDefinition2, 5);
					this.afterBattleDataByBody.Add(unitBodyData);
				}
				else
				{
					AfterBattleData unitBodyData2 = new AfterBattleData();
					unitBodyData2.Initialize(unitBody.Name, this.empireModifiers.AfterBattleDefinition, 5);
					this.afterBattleDataByBody.Add(unitBodyData2);
				}
			}
		}
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		this.modifierMaximalValue = new AIParameter.AIModifier[this.empireModifiers.AfterBattleDefinition.AIModifiers.Length];
		for (int i = 0; i < this.empireModifiers.AfterBattleDefinition.AIModifiers.Length; i++)
		{
			this.itemDataDecisionMaker.RegisterOutput(this.empireModifiers.AfterBattleDefinition.AIModifiers[i].Name);
			this.modifierMaximalValue[i] = new AIParameter.AIModifier();
			this.modifierMaximalValue[i].Name = this.empireModifiers.AfterBattleDefinition.AIModifiers[i].Name;
			this.modifierMaximalValue[i].Value = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/MaximumModifierValues/{1}", AILayer_UnitDesigner.RegistryPath, this.empireModifiers.AfterBattleDefinition.AIModifiers[i].Name), 1f);
		}
		this.modifierNavalMaximalValue = new AIParameter.AIModifier[this.empireModifiers.AfterBattleDefinition.AIModifiers.Length];
		for (int j = 0; j < this.empireModifiers.AfterBattleDefinition.AIModifiers.Length; j++)
		{
			this.itemDataDecisionMaker.RegisterOutput(this.empireModifiers.AfterBattleDefinition.AIModifiers[j].Name);
			this.modifierNavalMaximalValue[j] = new AIParameter.AIModifier();
			this.modifierNavalMaximalValue[j].Name = this.empireModifiers.AfterBattleDefinition.AIModifiers[j].Name;
			this.modifierNavalMaximalValue[j].Value = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/NavalMaximumModifierValues/{1}", AILayer_UnitDesigner.RegistryPath, this.empireModifiers.AfterBattleDefinition.AIModifiers[j].Name), 1f);
		}
		this.RunBattleAnalzer();
		this.VictoryLayer = base.AIEntity.GetLayer<AILayer_Victory>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.playerRepositoryService = this.game.Services.GetService<IPlayerRepositoryService>();
		yield break;
	}

	public override void Release()
	{
		if (this.aiLayerEncounter != null)
		{
			this.aiLayerEncounter.EncounterRoundUpdate -= this.AILayer_Encounter_EncounterRoundUpdate;
			this.aiLayerEncounter = null;
		}
		if (this.itemDataDecisionMaker != null)
		{
			this.itemDataDecisionMaker.ParameterContextModifierDelegate = null;
			this.itemDataDecisionMaker.ParameterContextNormalizationDelegate = null;
			this.itemDataDecisionMaker.ScoreTransferFunctionDelegate = null;
			this.itemDataDecisionMaker = null;
		}
		if (this.departmentOfDefense != null)
		{
			this.departmentOfDefense.AvailableUnitBodyChanged -= this.DepartmentOfDefense_AvailableUnitBodyChanged;
			this.departmentOfDefense = null;
		}
		this.departmentOfEducation = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfForeignAffairs = null;
		this.itemDataRepository = null;
		this.personalityHelper = null;
		this.endTurnService = null;
		this.aiDataRepositoryHelper = null;
		this.unitDesignRepository = null;
		this.empireDataHelper = null;
		this.afterBattleDataByBody.Clear();
		base.Release();
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		foreach (string text in this.ExpectedResourceUsage.Keys.ToList<string>())
		{
			float value = 0f;
			this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, text, out value, false);
			this.ExpectedResourceUsage[text] = value;
		}
		this.ResourceLimitMultiplier = Mathf.Max(3f, (float)this.departmentOfTheInterior.Cities.Count / 2f);
		base.CreateLocalNeeds(context, pass);
		List<ResourcePolicyMessage> list = new List<ResourcePolicyMessage>();
		list.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<ResourcePolicyMessage>(BlackboardLayerID.Empire));
		if (list.Count == 0 && this.resourcePolicy == null)
		{
			AILayer.LogError("Missing resource policy message in Empire {0}", new object[]
			{
				base.AIEntity.Empire.Index
			});
			AIScoring aiscoring = new AIScoring();
			aiscoring.Name = DepartmentOfTheTreasury.Resources.Production;
			aiscoring.Value = 1f;
			this.resourcePolicy = new List<AIScoring>();
			this.resourcePolicy.Add(aiscoring);
		}
		else
		{
			this.resourcePolicy = list[0].ResourcePolicyForUnitDesign;
		}
		this.UpdateUnitTypeBasedModifier();
		this.GatherBestTierItem();
		this.currentResources = new float[this.resourcePolicy.Count];
		for (int i = 0; i < this.currentResources.Length; i++)
		{
			this.currentResources[i] = 0f;
		}
		this.RunBattleAnalzer();
		this.heroDesignToImprove.Clear();
		for (int j = 0; j < this.departmentOfEducation.Heroes.Count; j++)
		{
			this.GenerateHeroRetrofitNeeds(this.departmentOfEducation.Heroes[j]);
		}
		List<UnitDesign> list2 = this.departmentOfDefense.UnitDesignDatabase.GetUserDefinedUnitDesignsAsEnumerable().ToList<UnitDesign>();
		list2.Sort((UnitDesign left, UnitDesign right) => -1 * left.Tags.Contains(DownloadableContent9.TagColossus).CompareTo(right.Tags.Contains(DownloadableContent9.TagColossus)));
		foreach (UnitDesign unitDesign in list2)
		{
			if (!unitDesign.Hidden)
			{
				if (unitDesign.UnitEquipmentSet == null || unitDesign.UnitEquipmentSet.Slots == null)
				{
					Diagnostics.LogWarning("Invalid empty equipment... {0}", new object[]
					{
						unitDesign.Name
					});
				}
				else
				{
					try
					{
						this.GenerateNewUnitDesignFor(unitDesign);
					}
					catch (Exception ex)
					{
						Diagnostics.LogError("exception: {0}", new object[]
						{
							ex.ToString()
						});
					}
				}
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		if (this.unitDesignToImprove.Count > 0)
		{
			this.synchronousJobRepositoryHelper.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_EditUnitDesign));
		}
		ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
		service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_RetrofitHeroDesign));
	}

	private bool CheckItemDefinitionAgainstResourceAvailability(ItemDefinition itemDefinition)
	{
		for (int i = 0; i < itemDefinition.Costs.Length; i++)
		{
			if (!(itemDefinition.Costs[i].ResourceName == DepartmentOfTheTreasury.Resources.Production))
			{
				float num;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, itemDefinition.Costs[i].ResourceName, out num, true))
				{
					num = 1f;
				}
				if (num < 1f)
				{
					return false;
				}
			}
		}
		return true;
	}

	private float ComputeUnitDesignScore(UnitDesign unitDesign)
	{
		this.currentAfterBattleData = this.GetOrCreateAfterBattleData(unitDesign.UnitBodyDefinition.Name);
		this.itemDataDecisionMaker.Context.SimulationObject = unitDesign.Context;
		this.isCurrentDesignSeafaring = unitDesign.CheckAgainstTag(DownloadableContent16.SeafaringUnit);
		AIUnitDesignData aiunitDesignData;
		if (this.unitDesignRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, unitDesign.Model, out aiunitDesignData))
		{
			aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot = new string[unitDesign.UnitEquipmentSet.Slots.Length];
			aiunitDesignData.OldUnitDesignScoring.ItemScorePerSlot = new float[unitDesign.UnitEquipmentSet.Slots.Length];
		}
		float num = 0f;
		AIItemData aiitemData = null;
		for (int i = 0; i < unitDesign.UnitEquipmentSet.Slots.Length; i++)
		{
			StaticString staticString = unitDesign.UnitEquipmentSet.Slots[i].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
			if (this.itemDataRepository.TryGet(staticString, out aiitemData) && this.IsAtMainSlot(unitDesign.UnitEquipmentSet, aiitemData.ItemDefinition, i))
			{
				DecisionResult decisionResult = this.itemDataDecisionMaker.EvaluateDecision(aiitemData);
				num += decisionResult.Score;
				if (aiunitDesignData != null)
				{
					aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot[i] = staticString;
					aiunitDesignData.OldUnitDesignScoring.ItemScorePerSlot[i] = decisionResult.Score;
				}
			}
			else if (unitDesign.UnitEquipmentSet.Slots[i].SlotType == "Accessory")
			{
				this.EmptySlot = true;
			}
		}
		if (aiunitDesignData != null)
		{
			aiunitDesignData.OldUnitDesignScoring.GlobalScore = num;
		}
		return num;
	}

	private void DepartmentOfDefense_AvailableUnitBodyChanged(object sender, ConstructibleElementEventArgs e)
	{
		this.GetOrCreateAfterBattleData(e.ConstructibleElement.Name);
	}

	private void FilterAvailableItems(UnitEquipmentSet unitEquipmentSet, Unit unit)
	{
		this.availableItemData.Clear();
		foreach (AIItemData aiitemData in this.itemDataRepository)
		{
			if (!aiitemData.ItemDefinition.Hidden && aiitemData.IsResearched(base.AIEntity.Empire.Index))
			{
				AIUnitDesignData aiunitDesignData;
				if (this.Hero != null && this.HeroDesign && this.unitDesignRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, this.Hero.UnitDesign.Model, out aiunitDesignData) && aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot.Contains(aiitemData.ToString()))
				{
					this.availableItemData.Add(aiitemData);
				}
				else if (this.CheckItemDefinitionAgainstResourceAvailability(aiitemData.ItemDefinition))
				{
					bool flag = false;
					for (int i = 0; i < aiitemData.ItemDefinition.Slots.Length; i++)
					{
						bool flag2 = true;
						for (int j = 0; j < aiitemData.ItemDefinition.Slots[i].SlotTypes.Length; j++)
						{
							if (!this.HasSlotType(unitEquipmentSet, aiitemData.ItemDefinition.Slots[i].SlotTypes[j]))
							{
								flag2 = false;
								break;
							}
						}
						if (flag2)
						{
							flag = true;
							break;
						}
					}
					if (flag && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, aiitemData.ItemDefinition, new string[]
					{
						"SlotPrerequisite"
					}))
					{
						this.availableItemData.Add(aiitemData);
					}
				}
			}
		}
	}

	private int FindBestOneSlotItemInList(int startingIndex, Unit dummyUnit, UnitEquipmentSet unitEquipmentSet, int equipmentIndexToFill)
	{
		StaticString slotType = unitEquipmentSet.Slots[equipmentIndexToFill].SlotType;
		for (int i = startingIndex; i < this.decisionResult.Count; i++)
		{
			AIItemData aiitemData = this.decisionResult[i].Element as AIItemData;
			if (aiitemData != null)
			{
				int num = -1;
				for (int j = 0; j < aiitemData.ItemDefinition.Slots.Length; j++)
				{
					if (aiitemData.ItemDefinition.Slots[j].SlotType == slotType && aiitemData.ItemDefinition.Slots[j].SlotTypes.Length == 1)
					{
						num = j;
						break;
					}
				}
				if (num >= 0)
				{
					if (this.departmentOfDefense.IsEquipmentItemPrerequisitesValid(dummyUnit, aiitemData.ItemDefinition))
					{
						return i;
					}
				}
			}
		}
		return -1;
	}

	private int FindFirstEmptySlotIndex(int[] itemDataBySlot, UnitEquipmentSet unitEquipmentSet, StaticString slotName, Unit unit)
	{
		int num = 0;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		int i = 0;
		while (i < unitEquipmentSet.Slots.Length)
		{
			if (!(unitEquipmentSet.Slots[i].SlotType == UnitEquipmentSet.AccessoryType))
			{
				goto IL_41;
			}
			if ((float)num < propertyValue)
			{
				num++;
				goto IL_41;
			}
			IL_3B:
			i++;
			continue;
			IL_41:
			if (unitEquipmentSet.Slots[i].SlotType == slotName && itemDataBySlot[i] < 0)
			{
				return i;
			}
			goto IL_3B;
		}
		return -1;
	}

	private int FindEquipmentSlot(int[] itemDataBySlot, UnitEquipmentSet unitEquipmentSet, StaticString slotName, Unit unit)
	{
		int num = 0;
		float propertyValue = unit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount);
		int num2 = -1;
		int i = 0;
		while (i < unitEquipmentSet.Slots.Length)
		{
			if (!(unitEquipmentSet.Slots[i].SlotType == UnitEquipmentSet.AccessoryType))
			{
				goto IL_43;
			}
			if ((float)num < propertyValue)
			{
				num++;
				goto IL_43;
			}
			IL_3D:
			i++;
			continue;
			IL_43:
			if (!(slotName == unitEquipmentSet.Slots[i].SlotType))
			{
				goto IL_3D;
			}
			if (itemDataBySlot[i] < 0)
			{
				return i;
			}
			if (num2 < 0)
			{
				num2 = i;
				goto IL_3D;
			}
			goto IL_3D;
		}
		return num2;
	}

	private void GatherBestTierItem()
	{
		this.bestTierByResourceName.Clear();
		foreach (AIItemData aiitemData in this.itemDataRepository)
		{
			if (!aiitemData.ItemDefinition.Hidden)
			{
				if (aiitemData.IsResearched(base.AIEntity.Empire.Index))
				{
					if (!StaticString.IsNullOrEmpty(aiitemData.ResourceName) && !StaticString.IsNullOrEmpty(aiitemData.Tier))
					{
						if (this.bestTierByResourceName.ContainsKey(aiitemData.ResourceName))
						{
							if (this.bestTierByResourceName[aiitemData.ResourceName].CompareTo(aiitemData.Tier) < 0)
							{
								this.bestTierByResourceName[aiitemData.ResourceName] = aiitemData.Tier;
							}
						}
						else
						{
							this.bestTierByResourceName.Add(aiitemData.ResourceName, aiitemData.Tier);
						}
					}
				}
			}
		}
	}

	private void GenerateHeroRetrofitNeeds(Unit hero)
	{
		this.unitAssignationParameterModifier.Clear();
		AIData_Unit unitData;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(hero.GUID, out unitData))
		{
			for (int i = 0; i < unitData.HeroData.LongTermSpecialtyFitness.Length; i++)
			{
				this.unitAssignationParameterModifier.Add(new AIParameter.AIModifier(AILayer_HeroAssignation.HeroAssignationTypeNames[i], unitData.HeroData.LongTermSpecialtyFitness[i]));
			}
		}
		float num = 0.4f;
		if (hero.Garrison != null)
		{
			this.HeroDesign = true;
			num = 0.75f;
			AIData_GameEntity aidata_GameEntity;
			if (this.aiDataRepositoryHelper.TryGetAIData(hero.Garrison.GUID, out aidata_GameEntity) && aidata_GameEntity is ICommanderMissionProvider)
			{
				AICommanderMission commanderMission = (aidata_GameEntity as ICommanderMissionProvider).CommanderMission;
				if (commanderMission != null)
				{
					this.modifierBoost = commanderMission.GetHeroItemModifiers();
				}
			}
			if (hero.Garrison is City)
			{
				this.Governorhero = true;
				AIEntity_City entity = base.AIEntity.AIPlayer.GetEntity<AIEntity_City>((AIEntity_City match) => match.City.GUID == hero.Garrison.GUID);
				if (entity != null && entity.AICityState != null)
				{
					this.unitAssignationParameterModifier.AddRange(entity.AICityState.GetParameterModiferByName("HeroItem"));
				}
			}
			if (hero.Garrison is Army)
			{
				if (this.VictoryLayer.NeedPreachers || this.VictoryLayer.NeedSettlers)
				{
					string victorystring = "Settler";
					if (this.VictoryLayer.NeedPreachers)
					{
						victorystring = "Preacher";
					}
					if ((hero.Garrison as Army).StandardUnits.Count((Unit x) => x.UnitDesign.Name.ToString().Contains(victorystring)) > 5)
					{
						this.boostVictoryItem = true;
					}
				}
				this.HeroIsSlowpoke = true;
				float num2 = hero.GetPropertyValue(SimulationProperties.Movement);
				for (int j = 0; j < hero.UnitDesign.UnitEquipmentSet.Slots.Length; j++)
				{
					StaticString itemName = hero.UnitDesign.UnitEquipmentSet.Slots[j].ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
					AIItemData aiitemData;
					if (this.itemDataRepository.TryGet(itemName, out aiitemData) && aiitemData.ToString() == "ItemTalismanIronTier3")
					{
						num2 -= 3f;
						break;
					}
				}
				foreach (Unit unit in (hero.Garrison as Army).StandardUnits)
				{
					if (num2 - unit.GetPropertyValue(SimulationProperties.Movement) > -1f)
					{
						this.HeroIsSlowpoke = false;
						break;
					}
				}
			}
			if (hero.Garrison is SpiedGarrison)
			{
				this.Spyhero = true;
			}
		}
		this.IgnoreCostForCurrentDesign = true;
		this.Hero = hero;
		this.CurrentUnitDesign = hero.UnitDesign;
		this.EmptySlot = false;
		float num3 = this.ComputeUnitDesignScore(hero.UnitDesign);
		if (this.EmptySlot && this.HeroDesign)
		{
			num = 0.9f;
		}
		this.SetItems.Clear();
		this.IgnoreCostForCurrentDesign = false;
		UnitDesign unitDesign;
		float num4 = this.GenerateNewUnitDesign(hero.UnitDesign, out unitDesign);
		this.SetItems.Clear();
		this.CurrentUnitDesign = null;
		this.Hero = null;
		this.Governorhero = false;
		this.HeroDesign = false;
		this.HeroIsSlowpoke = false;
		this.Spyhero = false;
		this.boostVictoryItem = false;
		this.modifierBoost = null;
		if (num3 < num4 * num)
		{
			bool flag = false;
			for (int k = 0; k < unitDesign.UnitEquipmentSet.Slots.Length; k++)
			{
				if (unitDesign.UnitEquipmentSet.Slots[k].ItemName != hero.UnitDesign.UnitEquipmentSet.Slots[k].ItemName)
				{
					flag = true;
					break;
				}
			}
			if (flag && this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(hero.GUID, out unitData))
			{
				unitData.RetrofitData.RetrofitCosts = this.departmentOfDefense.GetRetrofitCosts(hero, unitDesign);
				unitData.RetrofitData.MayRetrofit = true;
				EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire, (EvaluableMessage_RetrofitUnit match) => match.ElementGuid == unitData.Unit.GUID);
				if (evaluableMessage_RetrofitUnit == null || evaluableMessage_RetrofitUnit.State != BlackboardMessage.StateValue.Message_InProgress)
				{
					evaluableMessage_RetrofitUnit = new EvaluableMessage_RetrofitUnit(hero.GUID);
					base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_RetrofitUnit);
				}
				float num5 = 0f;
				for (int l = 0; l < unitData.RetrofitData.RetrofitCosts.Length; l++)
				{
					if (unitData.RetrofitData.RetrofitCosts[l].ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
					{
						num5 += unitData.RetrofitData.RetrofitCosts[l].Value;
					}
				}
				float globalMotivation = 0.9f;
				float num6 = 0f;
				if (num4 > 0f)
				{
					num6 = (num4 - num3) / num4;
				}
				if (num6 > 1f)
				{
					globalMotivation = 1f;
				}
				num6 = Mathf.Clamp01(num6);
				num6 = num6 * 0.5f + 0.5f;
				float num7 = 0.5f;
				num7 = AILayer.Boost(num7, num6);
				evaluableMessage_RetrofitUnit.SetInterest(globalMotivation, num7);
				evaluableMessage_RetrofitUnit.UpdateBuyEvaluation("Retrofit", 0UL, num5, 2, 0f, 0UL);
				evaluableMessage_RetrofitUnit.TimeOut = 1;
				this.heroDesignToImprove.Add(unitDesign);
			}
		}
	}

	private float GenerateNewUnitDesign(UnitDesign originalUnitDesign, out UnitDesign newUnitDesign)
	{
		bool flag = originalUnitDesign.CheckUnitAbility(UnitAbility.ReadonlyDualWield, -1);
		newUnitDesign = (originalUnitDesign.Clone() as UnitDesign);
		newUnitDesign.UnitEquipmentSet = null;
		newUnitDesign.XmlSerializableUnitEquipmentSet = null;
		Unit unit = this.departmentOfDefense.CreateDummyUnitInTemporaryGarrison();
		DepartmentOfDefense.ApplyUnitDesignToUnit(unit, newUnitDesign);
		for (int i = 0; i < this.currentResources.Length; i++)
		{
			this.currentResources[i] = 0f;
		}
		int[] array = new int[newUnitDesign.UnitBodyDefinition.UnitEquipmentSet.Slots.Length];
		for (int j = 0; j < array.Length; j++)
		{
			array[j] = -1;
		}
		this.currentAfterBattleData = this.GetOrCreateAfterBattleData(originalUnitDesign.UnitBodyDefinition.Name);
		this.isCurrentDesignSeafaring = originalUnitDesign.CheckAgainstTag(DownloadableContent16.SeafaringUnit);
		AIUnitDesignData aiunitDesignData;
		if (this.unitDesignRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, originalUnitDesign.Model, out aiunitDesignData))
		{
			aiunitDesignData.NewUnitDesignScoring.ItemNamePerSlot = new string[originalUnitDesign.UnitEquipmentSet.Slots.Length];
			aiunitDesignData.NewUnitDesignScoring.ItemScorePerSlot = new float[originalUnitDesign.UnitEquipmentSet.Slots.Length];
		}
		this.itemDataDecisionMaker.Context.SimulationObject = unit;
		this.decisionResult.Clear();
		this.FilterAvailableItems(newUnitDesign.UnitEquipmentSet, unit);
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && aiunitDesignData != null)
		{
			DecisionMakerEvaluationData<AIItemData, InterpreterContext> decisionMakerEvaluationData;
			this.itemDataDecisionMaker.EvaluateDecisions(this.availableItemData, ref this.decisionResult, out decisionMakerEvaluationData);
			decisionMakerEvaluationData.Turn = this.game.Turn;
			decisionMakerEvaluationData.Title = string.Format("Improve {0}", originalUnitDesign.Name);
			aiunitDesignData.DecisionMakerEvaluationDataHistoric.Add(decisionMakerEvaluationData);
		}
		else
		{
			this.itemDataDecisionMaker.EvaluateDecisions(this.availableItemData, ref this.decisionResult);
		}
		float num = 0f;
		if (this.HeroDesign && this.Hero != null && this.Hero.GetPropertyValue(SimulationProperties.AccessoriesSlotCount) > unit.GetPropertyValue(SimulationProperties.AccessoriesSlotCount))
		{
			unit.SetPropertyBaseValue(SimulationProperties.AccessoriesSlotCount, unit.GetPropertyBaseValue(SimulationProperties.AccessoriesSlotCount) + 1f);
			unit.Refresh(true);
		}
		using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(unit))
		{
			for (int k = 0; k < this.decisionResult.Count; k++)
			{
				if (this.IsAllSlotFilled(array))
				{
					break;
				}
				if (this.decisionResult[k].Score <= this.scoreLimit)
				{
					break;
				}
				AIItemData aiitemData = this.decisionResult[k].Element as AIItemData;
				bool flag2 = flag && (aiitemData.ItemDefinition.SubCategory == "SubCategorySword1" || aiitemData.ItemDefinition.SubCategory == "SubCategoryAxe1" || aiitemData.ItemDefinition.SubCategory == "SubCategoryHammer1");
				int l;
				if (aiitemData != null && this.HasAtLeastOneSlotEmptyAndNoUnknowSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition, unit, out l) && this.departmentOfDefense.IsEquipmentItemPrerequisitesValid(unit, aiitemData.ItemDefinition))
				{
					for (l = 0; l < aiitemData.ItemDefinition.Slots.Length; l++)
					{
						if (flag2)
						{
							bool flag3 = false;
							foreach (UnitAbilityReference unitAbilityReference in aiitemData.ItemDefinition.AbilityReferences)
							{
								flag3 = (flag3 || unitAbilityReference.ToString().Contains("TwinBlades"));
								if (this.SetItems.Contains(unitAbilityReference.ToString()) && (!flag3 || unitAbilityReference.ToString() != "UnitAbilityWarriorSlayer"))
								{
									goto IL_A16;
								}
							}
						}
						int num2 = this.FindFirstEmptySlotIndex(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotType, unit);
						if (num2 >= 0)
						{
							if (aiitemData.ItemDefinition.Slots[l].Prerequisites != null)
							{
								bool flag4 = true;
								for (int n = 0; n < aiitemData.ItemDefinition.Slots[l].Prerequisites.Length; n++)
								{
									if (!aiitemData.ItemDefinition.Slots[l].Prerequisites[n].Check(interpreterSession.Context))
									{
										flag4 = false;
										break;
									}
								}
								if (!flag4)
								{
									goto IL_9FB;
								}
							}
							if (this.CheckAgainstResourceItemNeed(aiitemData) || this.CurrentUnitDesign.Tags.Contains(DownloadableContent9.TagColossus))
							{
								if (aiitemData.ItemDefinition.Slots[l].SlotTypes.Length > 1)
								{
									float num3 = 0f;
									for (int num4 = 0; num4 < aiitemData.ItemDefinition.Slots[l].SlotTypes.Length; num4++)
									{
										num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotTypes[num4], unit);
										if (num2 < 0)
										{
											num3 = float.MaxValue;
											break;
										}
										int num5 = array[num2];
										int num6 = 0;
										if (num5 < 0)
										{
											num5 = this.FindBestOneSlotItemInList(k + 1, unit, newUnitDesign.UnitEquipmentSet, num2);
										}
										if (num5 >= 0)
										{
											AIItemData aiitemData2 = this.decisionResult[num5].Element as AIItemData;
											if (aiitemData2 != null && aiitemData2.ItemDefinition.Slots[num6].SlotType == aiitemData.ItemDefinition.Slots[l].SlotTypes[num4])
											{
												num3 += this.decisionResult[num5].Score;
											}
										}
									}
									if (num3 > this.decisionResult[k].Score)
									{
										goto IL_9FB;
									}
								}
								DepartmentOfDefense.RemoveEquipmentSet(unit);
								num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotType, unit);
								string text = aiitemData.ItemDefinition.Name + DepartmentOfDefense.ItemSeparators[0].ToString() + num2;
								for (int num7 = 0; num7 < aiitemData.ItemDefinition.Slots[l].SlotTypes.Length; num7++)
								{
									num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotTypes[num7], unit);
									int num8 = array[num2];
									if (num8 >= 0)
									{
										int num9 = 0;
										while (num9 < array.Length)
										{
											if (array[num9] == num8)
											{
												array[num2] = -1;
												newUnitDesign.UnitEquipmentSet.Slots[num2].ItemName = string.Empty;
												if (aiunitDesignData != null)
												{
													aiunitDesignData.NewUnitDesignScoring.ItemNamePerSlot[num2] = string.Empty;
													aiunitDesignData.NewUnitDesignScoring.ItemScorePerSlot[num2] = 0f;
												}
											}
											k++;
										}
										AIItemData aiitemData3 = this.decisionResult[num8].Element as AIItemData;
										num -= this.decisionResult[num8].Score;
										for (int num10 = 0; num10 < aiitemData3.ItemDefinition.Costs.Length; num10++)
										{
											for (int num11 = 0; num11 < this.currentResources.Length; num11++)
											{
												if (aiitemData3.ItemDefinition.Costs[num10].ResourceName == this.resourcePolicy[num11].Name)
												{
													this.currentResources[num11] -= aiitemData3.ItemDefinition.Costs[num10].GetValue(base.AIEntity.Empire);
													break;
												}
											}
										}
									}
									array[num2] = k;
									newUnitDesign.UnitEquipmentSet.Slots[num2].ItemName = text;
									if (aiunitDesignData != null)
									{
										aiunitDesignData.NewUnitDesignScoring.ItemNamePerSlot[num2] = text;
										aiunitDesignData.NewUnitDesignScoring.ItemScorePerSlot[num2] = this.decisionResult[k].Score;
										if (flag2)
										{
											foreach (UnitAbilityReference unitAbilityReference2 in aiitemData.ItemDefinition.AbilityReferences)
											{
												this.SetItems.Add(unitAbilityReference2.ToString());
											}
										}
										if (this.Hero == null && !aiitemData.IsNaval && !this.CurrentUnitDesign.Tags.Contains(DownloadableContent9.TagSolitary))
										{
											for (int num12 = 0; num12 < aiitemData.ItemDefinition.Costs.Length; num12++)
											{
												if (aiitemData.ItemDefinition.Costs[num12].ResourceName.ToString().Contains("Strategic"))
												{
													Dictionary<string, float> expectedResourceUsage = this.ExpectedResourceUsage;
													string text2 = aiitemData.ItemDefinition.Costs[num12].ResourceName.ToString();
													Dictionary<string, float> dictionary = expectedResourceUsage;
													Dictionary<string, float> dictionary2 = dictionary;
													string key = text2;
													dictionary2[key] -= aiitemData.ItemDefinition.Costs[num12].GetValue(base.AIEntity.Empire) * this.ResourceLimitMultiplier;
												}
											}
										}
									}
								}
								num += this.decisionResult[k].Score;
								DepartmentOfDefense.ApplyEquipmentSet(unit);
								for (int num13 = 0; num13 < aiitemData.ItemDefinition.Costs.Length; num13++)
								{
									for (int num14 = 0; num14 < this.currentResources.Length; num14++)
									{
										if (aiitemData.ItemDefinition.Costs[num13].ResourceName == this.resourcePolicy[num14].Name)
										{
											this.currentResources[num14] += aiitemData.ItemDefinition.Costs[num13].GetValue(base.AIEntity.Empire);
											break;
										}
									}
								}
							}
						}
						IL_9FB:;
					}
				}
				IL_A16:;
			}
		}
		this.itemDataDecisionMaker.Context.SimulationObject = null;
		this.departmentOfDefense.DisposeDummyUnit(unit);
		if (aiunitDesignData != null)
		{
			aiunitDesignData.NewUnitDesignScoring.GlobalScore = num;
		}
		return num;
	}

	private bool CheckAgainstResourceItemNeed(AIItemData currentItemData)
	{
		float num;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num, false);
		AIUnitDesignData aiunitDesignData;
		if (this.Hero != null && this.HeroDesign && this.unitDesignRepository.TryGetUnitDesignData(base.AIEntity.Empire.Index, this.Hero.UnitDesign.Model, out aiunitDesignData) && aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot.Contains(currentItemData.ToString()))
		{
			return true;
		}
		for (int i = 0; i < currentItemData.ItemDefinition.Costs.Length; i++)
		{
			int j = 0;
			while (j < this.currentResources.Length)
			{
				if (currentItemData.ItemDefinition.Costs[i].ResourceName == this.resourcePolicy[j].Name)
				{
					float num2;
					float num3;
					if (this.resourcePolicy[j].Name == "EmpireMoney" && num > 200f)
					{
						num2 = this.resourcePolicy[j].Value + num / 25f;
					}
					else if (((this.HeroDesign && !this.Governorhero) || (this.Governorhero && currentItemData.ItemDefinition.Slots[0].SlotType == "Accessory")) && currentItemData.ItemDefinition.Costs[i].ResourceName.ToString().Contains("Strategic") && this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, currentItemData.ItemDefinition.Costs[i].ResourceName, out num3, false))
					{
						num2 = this.resourcePolicy[j].Value;
						float num4 = (currentItemData.ItemDefinition.Slots[0].SlotType == "Accessory") ? 3f : 4f;
						if (num3 > num4 * currentItemData.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) && num3 / 2f > num2)
						{
							num2 = num3 / 2f;
						}
					}
					else
					{
						num2 = this.resourcePolicy[j].Value;
					}
					if (currentItemData.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) + this.currentResources[j] >= num2)
					{
						return false;
					}
					break;
				}
				else
				{
					j++;
				}
			}
		}
		return true;
	}

	private void GenerateNewUnitDesignFor(UnitDesign originalUnitDesign)
	{
		Dictionary<string, float> dictionary = new Dictionary<string, float>(this.ExpectedResourceUsage);
		this.unitAssignationParameterModifier.Clear();
		this.CurrentUnitDesign = originalUnitDesign;
		this.OriginalNonHeroDesign = true;
		float num = this.ComputeUnitDesignScore(originalUnitDesign);
		this.OriginalNonHeroDesign = false;
		Dictionary<string, float> dictionary2 = new Dictionary<string, float>(this.ExpectedResourceUsage);
		this.ExpectedResourceUsage = new Dictionary<string, float>(dictionary);
		UnitDesign unitDesign;
		float num2 = this.GenerateNewUnitDesign(originalUnitDesign, out unitDesign);
		this.CurrentUnitDesign = null;
		bool flag = false;
		if (num < num2 * this.newUnitDesignScoreModifier)
		{
			for (int i = 0; i < unitDesign.UnitEquipmentSet.Slots.Length; i++)
			{
				if (unitDesign.UnitEquipmentSet.Slots[i].ItemName != originalUnitDesign.UnitEquipmentSet.Slots[i].ItemName)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				this.unitDesignToImprove.Add(unitDesign);
			}
		}
		if (!flag)
		{
			this.ExpectedResourceUsage = new Dictionary<string, float>(dictionary2);
		}
	}

	private AfterBattleData GetOrCreateAfterBattleData(StaticString unitBodyName)
	{
		for (int i = 0; i < this.afterBattleDataByBody.Count; i++)
		{
			if (this.afterBattleDataByBody[i].UnitBodyName == unitBodyName)
			{
				return this.afterBattleDataByBody[i];
			}
		}
		AfterBattleData afterBattleData = new AfterBattleData();
		AfterBattleDefinition afterBattleDefinition;
		if (this.afterBattleDatabase.TryGetValue(unitBodyName, out afterBattleDefinition))
		{
			afterBattleData.Initialize(afterBattleDefinition, 5);
		}
		else
		{
			afterBattleData.Initialize(unitBodyName, this.empireModifiers.AfterBattleDefinition, 5);
		}
		afterBattleData.TurnUpdate(this.endTurnService.Turn);
		this.afterBattleDataByBody.Add(afterBattleData);
		return afterBattleData;
	}

	private bool HasAtLeastOneSlotEmptyAndNoUnknowSlot(int[] itemDataBySlot, UnitEquipmentSet unitEquipmentSet, ItemDefinition itemDefinition, Unit unit, out int chosenSlotIndex)
	{
		bool flag = unit.UnitDesign.CheckUnitAbility(UnitAbility.ReadonlyDualWield, -1);
		chosenSlotIndex = -1;
		bool flag2 = false;
		for (int i = 0; i < itemDefinition.Slots.Length; i++)
		{
			int j = 0;
			while (j < itemDefinition.Slots[i].SlotTypes.Length)
			{
				int num = this.FindFirstEmptySlotIndex(itemDataBySlot, unitEquipmentSet, itemDefinition.Slots[i].SlotTypes[j], unit);
				if (num < 0)
				{
					if (flag)
					{
						flag2 = false;
						break;
					}
					return false;
				}
				else
				{
					if (itemDataBySlot[num] < 0)
					{
						flag2 = true;
					}
					j++;
				}
			}
			if (flag2)
			{
				chosenSlotIndex = i;
				break;
			}
		}
		return flag2;
	}

	private bool IsAllSlotFilled(int[] itemDataBySlot)
	{
		for (int i = 0; i < itemDataBySlot.Length; i++)
		{
			if (itemDataBySlot[i] < 0)
			{
				return false;
			}
		}
		return true;
	}

	private bool IsAtMainSlot(UnitEquipmentSet unitEquipmentSet, ItemDefinition itemDefinition, int equipmentIndex)
	{
		StaticString slotType = unitEquipmentSet.Slots[equipmentIndex].SlotType;
		for (int i = 0; i < itemDefinition.Slots.Length; i++)
		{
			if (itemDefinition.Slots[i].SlotType == slotType)
			{
				return true;
			}
		}
		return false;
	}

	private SynchronousJobState SynchronousJob_EditUnitDesign()
	{
		bool flag = false;
		if (this.unitDesignToImprove.Count == 0)
		{
			return SynchronousJobState.Failure;
		}
		for (int i = 0; i < this.unitDesignToImprove.Count; i++)
		{
			OrderEditUnitDesign order = new OrderEditUnitDesign(base.AIEntity.Empire.Index, this.unitDesignToImprove[i]);
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
			flag = true;
		}
		this.unitDesignToImprove.Clear();
		if (flag)
		{
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Failure;
	}

	private SynchronousJobState SynchronousJob_RetrofitHeroDesign()
	{
		if (this.currentEditHeroOrderTicket != null)
		{
			if (!this.currentEditHeroOrderTicket.Raised)
			{
				return SynchronousJobState.Running;
			}
			OrderEditHeroUnitDesign orderEditHeroUnitDesign = this.currentEditHeroOrderTicket.Order as OrderEditHeroUnitDesign;
			if (orderEditHeroUnitDesign != null)
			{
				int index = 0;
				Func<EvaluableMessage_RetrofitUnit, bool> <>9__0;
				while (index < this.departmentOfEducation.Heroes.Count)
				{
					if (this.departmentOfEducation.Heroes[index].UnitDesign.Model == orderEditHeroUnitDesign.UnitDesignModel)
					{
						Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
						BlackboardLayerID blackboardLayerID = BlackboardLayerID.Empire;
						BlackboardLayerID layerID = blackboardLayerID;
						Func<EvaluableMessage_RetrofitUnit, bool> filter;
						if ((filter = <>9__0) == null)
						{
							filter = (<>9__0 = ((EvaluableMessage_RetrofitUnit match) => match.ElementGuid == this.departmentOfEducation.Heroes[index].GUID));
						}
						EvaluableMessage_RetrofitUnit firstMessage = blackboard.GetFirstMessage<EvaluableMessage_RetrofitUnit>(layerID, filter);
						if (firstMessage == null)
						{
							break;
						}
						if (this.currentEditHeroOrderTicket.PostOrderResponse == PostOrderResponse.Processed)
						{
							firstMessage.SetObtained();
							break;
						}
						firstMessage.SetFailedToObtain();
						break;
					}
					else
					{
						int index2 = index;
						index = index2 + 1;
					}
				}
			}
			this.currentEditHeroOrderTicket = null;
		}
		bool flag = true;
		foreach (Player player in this.playerRepositoryService)
		{
			if (player.Type == PlayerType.Human && player.State != PlayerState.Ready)
			{
				flag = false;
				break;
			}
		}
		if (flag)
		{
			Diagnostics.Log("ELCP: SynchronousJob_RetrofitHeroDesign() SynchronousJob_ExecuteNeeds detected all humans are ready, aborting");
			foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
			{
				evaluableMessage_RetrofitUnit.SetFailedToObtain();
			}
			return SynchronousJobState.Failure;
		}
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit2 in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
		{
			AIData_Unit aidata_Unit;
			if (evaluableMessage_RetrofitUnit2.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(evaluableMessage_RetrofitUnit2.ElementGuid, out aidata_Unit) && aidata_Unit.Unit.UnitDesign is UnitProfile)
			{
				UnitDesign unitDesign = null;
				for (int i = 0; i < this.heroDesignToImprove.Count; i++)
				{
					if (this.heroDesignToImprove[i].Model == aidata_Unit.Unit.UnitDesign.Model)
					{
						unitDesign = this.heroDesignToImprove[i];
						this.heroDesignToImprove.RemoveAt(i);
						break;
					}
				}
				if (unitDesign == null)
				{
					evaluableMessage_RetrofitUnit2.SetFailedToObtain();
				}
				else
				{
					DepartmentOfDefense departmentOfDefense = agency;
					Unit unit = aidata_Unit.Unit;
					IConstructionCost[] retrofitCosts = agency.GetRetrofitCosts(aidata_Unit.Unit, unitDesign);
					IConstructionCost[] costs = retrofitCosts;
					bool flag2 = false;
					if ((base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitCultists7") || base.AIEntity.Empire.SimulationObject.Tags.Contains("FactionTraitMimics1")) && unit.Garrison is Army && !DepartmentOfEducation.IsLocked(unit))
					{
						flag2 = true;
					}
					if (unit.Garrison != null && unit.Garrison is SpiedGarrison && !DepartmentOfEducation.IsCaptured(unit))
					{
						flag2 = true;
					}
					if (unit.Garrison is Army && (this.VictoryLayer.NeedPreachers || this.VictoryLayer.NeedSettlers))
					{
						string victorystring = "Settler";
						if (this.VictoryLayer.NeedPreachers)
						{
							victorystring = "Preacher";
						}
						if ((unit.Garrison as Army).StandardUnits.Count((Unit x) => x.UnitDesign.Name.ToString().Contains(victorystring)) > 5)
						{
							flag2 = true;
						}
					}
					if (departmentOfDefense.CheckRetrofitPrerequisites(unit, costs) == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok || flag2)
					{
						OrderEditHeroUnitDesign orderEditHeroUnitDesign2 = new OrderEditHeroUnitDesign(base.AIEntity.Empire.Index, unitDesign);
						if (flag2)
						{
							orderEditHeroUnitDesign2.ForceEdit = true;
						}
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderEditHeroUnitDesign2, out this.currentEditHeroOrderTicket, null);
						return SynchronousJobState.Running;
					}
					evaluableMessage_RetrofitUnit2.SetFailedToObtain();
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private bool DontNeedCapacity(ItemDefinition itemdef)
	{
		if (this.CurrentUnitDesign != null && itemdef.AbilityReferences != null && itemdef.AbilityReferences.Length != 0)
		{
			List<UnitAbilityReference> list = new List<UnitAbilityReference>();
			UnitAbilityReference[] abilityReferences;
			if (this.CurrentUnitDesign.UnitEquipmentSet != null)
			{
				List<StaticString> list2 = new List<StaticString>(this.CurrentUnitDesign.UnitEquipmentSet.Slots.Length);
				IDatabase<ItemDefinition> database = Databases.GetDatabase<ItemDefinition>(false);
				Diagnostics.Assert(database != null);
				for (int i = 0; i < this.CurrentUnitDesign.UnitEquipmentSet.Slots.Length; i++)
				{
					UnitEquipmentSet.Slot slot = this.CurrentUnitDesign.UnitEquipmentSet.Slots[i];
					if (!list2.Contains(slot.ItemName))
					{
						StaticString key = slot.ItemName.ToString().Split(DepartmentOfDefense.ItemSeparators)[0];
						ItemDefinition itemDefinition;
						if (database.TryGetValue(key, out itemDefinition))
						{
							Diagnostics.Assert(itemDefinition != null);
							if (itemDefinition.AbilityReferences != null)
							{
								abilityReferences = itemDefinition.AbilityReferences;
								for (int j = 0; j < abilityReferences.Length; j++)
								{
									UnitAbilityReference reference = abilityReferences[j];
									if (itemdef.AbilityReferences.Any((UnitAbilityReference Match) => Match.Name == reference.Name))
									{
										if (itemDefinition.SubCategory != itemdef.SubCategory)
										{
											return true;
										}
										UnitAbilityReference unitAbilityReference = itemdef.AbilityReferences.First((UnitAbilityReference Match) => Match.Name == reference.Name);
										if (reference.Level > unitAbilityReference.Level)
										{
											return true;
										}
									}
								}
								list.AddRange(itemDefinition.AbilityReferences);
								list2.Add(slot.ItemName);
							}
						}
					}
				}
			}
			IDatabase<UnitAbility> unitAbilityDatatable = Databases.GetDatabase<UnitAbility>(false);
			Diagnostics.Assert(unitAbilityDatatable != null);
			List<UnitAbilityReference> list3 = this.CurrentUnitDesign.UnitBodyDefinition.UnitAbilities.ToList<UnitAbilityReference>();
			UnitProfile unitProfile = this.CurrentUnitDesign as UnitProfile;
			if (unitProfile != null)
			{
				list3.AddRange(unitProfile.ProfileAbilityReferences);
			}
			list3.RemoveAll((UnitAbilityReference match) => !unitAbilityDatatable.ContainsKey(match.Name) || unitAbilityDatatable.GetValue(match.Name).Hidden);
			abilityReferences = itemdef.AbilityReferences;
			for (int k = 0; k < abilityReferences.Length; k++)
			{
				UnitAbilityReference reference = abilityReferences[k];
				if (list3.Any((UnitAbilityReference Match) => Match.Name == reference.Name))
				{
					return true;
				}
			}
		}
		return false;
	}

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IAIEmpireDataAIHelper empireDataHelper;

	private List<AIParameter.AIModifier> unitSlayerModifier;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_UnitDesigner";

	private AfterBattleAnalyzer afterBattleAnalyzer;

	private IDatabase<AfterBattleDefinition> afterBattleDatabase;

	private List<AfterBattleData> afterBattleDataByBody;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AILayer_Encounter aiLayerEncounter;

	private List<AIItemData> availableItemData;

	private Dictionary<StaticString, string> bestTierByResourceName;

	private float costModifier;

	private AfterBattleData currentAfterBattleData;

	private bool isCurrentDesignSeafaring;

	private Ticket currentEditHeroOrderTicket;

	private float[] currentResources;

	private List<DecisionResult> decisionResult;

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private AfterBattleData empireModifiers;

	private IEndTurnService endTurnService;

	private global::Game game;

	private List<UnitDesign> heroDesignToImprove;

	private SimulationDecisionMaker<AIItemData> itemDataDecisionMaker;

	private IAIItemDataRepository itemDataRepository;

	private AIParameter.AIModifier[] modifierBoost;

	private AIParameter.AIModifier[] modifierMaximalValue;

	private AIParameter.AIModifier[] modifierNavalMaximalValue;

	private List<AIParameter.AIModifier> unitAssignationParameterModifier;

	private IPersonalityAIHelper personalityHelper;

	private List<AIScoring> resourcePolicy;

	private float scoreLimit;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepositoryHelper;

	private IAIUnitDesignDataRepository unitDesignRepository;

	private List<UnitDesign> unitDesignToImprove;

	private float newUnitDesignScoreModifier;

	private bool Governorhero;

	private bool HeroDesign;

	private bool HeroIsSlowpoke;

	private bool IgnoreCostForCurrentDesign;

	private Unit Hero;

	private bool Spyhero;

	private UnitDesign CurrentUnitDesign;

	private bool boostVictoryItem;

	private AILayer_Victory VictoryLayer;

	private List<string> SetItems;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private float ResourceLimitMultiplier;

	private Dictionary<string, float> ExpectedResourceUsage;

	private bool OriginalNonHeroDesign;

	private IPlayerRepositoryService playerRepositoryService;

	private bool EmptySlot;
}
