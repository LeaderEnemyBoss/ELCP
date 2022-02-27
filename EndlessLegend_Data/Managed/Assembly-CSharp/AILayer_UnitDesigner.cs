using System;
using System.Collections;
using System.Collections.Generic;
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
		this.unitSlayerModifier = new List<AIParameter.AIModifier>();
		base..ctor();
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
		if (!element.IsNaval)
		{
			if (element.ResourceName == "Iron" && this.bestTierByResourceName[element.ResourceName] != element.Tier)
			{
				currentScore = -2f;
				return currentScore;
			}
			float registryValue = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/TierModifier/{1}", AILayer_UnitDesigner.RegistryPath, element.Tier), 1f);
			currentScore *= registryValue;
		}
		float num = 0f;
		for (int i = 0; i < element.ItemDefinition.Costs.Length; i++)
		{
			bool flag = false;
			for (int j = 0; j < this.resourcePolicy.Count; j++)
			{
				if (this.resourcePolicy[j].Name == element.ItemDefinition.Costs[i].ResourceName)
				{
					num = Mathf.Min(1f, Mathf.Max(num, element.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire) / this.resourcePolicy[j].Value));
					flag = true;
					break;
				}
			}
			if (!flag)
			{
				num = 2f;
				break;
			}
		}
		return currentScore - num * this.costModifier;
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
		for (int index = 0; index < this.empireModifiers.AfterBattleDefinition.AIModifiers.Length; index++)
		{
			this.itemDataDecisionMaker.RegisterOutput(this.empireModifiers.AfterBattleDefinition.AIModifiers[index].Name);
			this.modifierMaximalValue[index] = new AIParameter.AIModifier();
			this.modifierMaximalValue[index].Name = this.empireModifiers.AfterBattleDefinition.AIModifiers[index].Name;
			this.modifierMaximalValue[index].Value = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/MaximumModifierValues/{1}", AILayer_UnitDesigner.RegistryPath, this.empireModifiers.AfterBattleDefinition.AIModifiers[index].Name), 1f);
		}
		this.modifierNavalMaximalValue = new AIParameter.AIModifier[this.empireModifiers.AfterBattleDefinition.AIModifiers.Length];
		for (int index2 = 0; index2 < this.empireModifiers.AfterBattleDefinition.AIModifiers.Length; index2++)
		{
			this.itemDataDecisionMaker.RegisterOutput(this.empireModifiers.AfterBattleDefinition.AIModifiers[index2].Name);
			this.modifierNavalMaximalValue[index2] = new AIParameter.AIModifier();
			this.modifierNavalMaximalValue[index2].Name = this.empireModifiers.AfterBattleDefinition.AIModifiers[index2].Name;
			this.modifierNavalMaximalValue[index2].Value = this.personalityHelper.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/NavalMaximumModifierValues/{1}", AILayer_UnitDesigner.RegistryPath, this.empireModifiers.AfterBattleDefinition.AIModifiers[index2].Name), 1f);
		}
		this.RunBattleAnalzer();
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
		foreach (UnitDesign unitDesign in this.departmentOfDefense.UnitDesignDatabase.GetUserDefinedUnitDesignsAsEnumerable())
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
			if (this.itemDataRepository.TryGet(staticString, out aiitemData))
			{
				if (this.IsAtMainSlot(unitDesign.UnitEquipmentSet, aiitemData.ItemDefinition, i))
				{
					DecisionResult decisionResult = this.itemDataDecisionMaker.EvaluateDecision(aiitemData);
					num += decisionResult.Score;
					if (aiunitDesignData != null)
					{
						aiunitDesignData.OldUnitDesignScoring.ItemNamePerSlot[i] = staticString;
						aiunitDesignData.OldUnitDesignScoring.ItemScorePerSlot[i] = decisionResult.Score;
					}
				}
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
			if (!aiitemData.ItemDefinition.Hidden)
			{
				if (aiitemData.IsResearched(base.AIEntity.Empire.Index))
				{
					if (this.CheckItemDefinitionAgainstResourceAvailability(aiitemData.ItemDefinition))
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
						if (flag)
						{
							if (DepartmentOfTheTreasury.CheckConstructiblePrerequisites(unit, aiitemData.ItemDefinition, new string[]
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
				goto IL_47;
			}
			if ((float)num < propertyValue)
			{
				num++;
				goto IL_47;
			}
			IL_6E:
			i++;
			continue;
			IL_47:
			if (unitEquipmentSet.Slots[i].SlotType == slotName && itemDataBySlot[i] < 0)
			{
				return i;
			}
			goto IL_6E;
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
				goto IL_49;
			}
			if ((float)num < propertyValue)
			{
				num++;
				goto IL_49;
			}
			IL_79:
			i++;
			continue;
			IL_49:
			if (!(slotName == unitEquipmentSet.Slots[i].SlotType))
			{
				goto IL_79;
			}
			if (itemDataBySlot[i] < 0)
			{
				return i;
			}
			if (num2 < 0)
			{
				num2 = i;
				goto IL_79;
			}
			goto IL_79;
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
		if (hero.Garrison != null)
		{
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
				AIEntity_City entity = base.AIEntity.AIPlayer.GetEntity<AIEntity_City>((AIEntity_City match) => match.City.GUID == hero.Garrison.GUID);
				if (entity != null && entity.AICityState != null)
				{
					this.unitAssignationParameterModifier.AddRange(entity.AICityState.GetParameterModiferByName("HeroItem"));
				}
			}
		}
		float num = 0.5f;
		float num2 = hero.GetPropertyValue(SimulationProperties.MaximumAssignmentCooldown) - hero.GetPropertyValue(SimulationProperties.AssignmentCooldown);
		if (num2 <= 1f)
		{
			num = 0.75f;
		}
		float num3 = this.ComputeUnitDesignScore(hero.UnitDesign);
		UnitDesign unitDesign;
		float num4 = this.GenerateNewUnitDesign(hero.UnitDesign, out unitDesign);
		num4 *= num;
		if (num3 < num4)
		{
			bool flag = false;
			for (int j = 0; j < unitDesign.UnitEquipmentSet.Slots.Length; j++)
			{
				if (unitDesign.UnitEquipmentSet.Slots[j].ItemName != hero.UnitDesign.UnitEquipmentSet.Slots[j].ItemName)
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
				for (int k = 0; k < unitData.RetrofitData.RetrofitCosts.Length; k++)
				{
					if (unitData.RetrofitData.RetrofitCosts[k].ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
					{
						num5 += unitData.RetrofitData.RetrofitCosts[k].Value;
					}
				}
				float globalMotivation = 0.8f;
				float num6 = 0f;
				if (num4 > 0f)
				{
					num6 = (num4 - num3) / num4;
				}
				if (num6 > 1f)
				{
					globalMotivation = 0.9f;
				}
				num6 = Mathf.Clamp01(num6);
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
				if (aiitemData != null)
				{
					int l;
					if (this.HasAtLeastOneSlotEmptyAndNoUnknowSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition, unit, out l))
					{
						if (this.departmentOfDefense.IsEquipmentItemPrerequisitesValid(unit, aiitemData.ItemDefinition))
						{
							for (l = 0; l < aiitemData.ItemDefinition.Slots.Length; l++)
							{
								int num2 = this.FindFirstEmptySlotIndex(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotType, unit);
								if (num2 >= 0)
								{
									if (aiitemData.ItemDefinition.Slots[l].Prerequisites != null)
									{
										bool flag = true;
										for (int m = 0; m < aiitemData.ItemDefinition.Slots[l].Prerequisites.Length; m++)
										{
											if (!aiitemData.ItemDefinition.Slots[l].Prerequisites[m].Check(interpreterSession.Context))
											{
												flag = false;
												break;
											}
										}
										if (!flag)
										{
											goto IL_815;
										}
									}
									if (this.CheckAgainstResourceItemNeed(aiitemData))
									{
										if (aiitemData.ItemDefinition.Slots[l].SlotTypes.Length > 1)
										{
											float num3 = 0f;
											for (int n = 0; n < aiitemData.ItemDefinition.Slots[l].SlotTypes.Length; n++)
											{
												num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotTypes[n], unit);
												if (num2 < 0)
												{
													num3 = float.MaxValue;
													break;
												}
												int num4 = array[num2];
												int num5 = 0;
												if (num4 < 0)
												{
													num4 = this.FindBestOneSlotItemInList(k + 1, unit, newUnitDesign.UnitEquipmentSet, num2);
												}
												if (num4 >= 0)
												{
													AIItemData aiitemData2 = this.decisionResult[num4].Element as AIItemData;
													if (aiitemData2 != null)
													{
														if (!(aiitemData2.ItemDefinition.Slots[num5].SlotType != aiitemData.ItemDefinition.Slots[l].SlotTypes[n]))
														{
															num3 += this.decisionResult[num4].Score;
														}
													}
												}
											}
											if (num3 > this.decisionResult[k].Score)
											{
												goto IL_815;
											}
										}
										DepartmentOfDefense.RemoveEquipmentSet(unit);
										num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotType, unit);
										string text = aiitemData.ItemDefinition.Name + DepartmentOfDefense.ItemSeparators[0] + num2;
										for (int num6 = 0; num6 < aiitemData.ItemDefinition.Slots[l].SlotTypes.Length; num6++)
										{
											num2 = this.FindEquipmentSlot(array, newUnitDesign.UnitEquipmentSet, aiitemData.ItemDefinition.Slots[l].SlotTypes[num6], unit);
											int num7 = array[num2];
											if (num7 >= 0)
											{
												int num8 = 0;
												while (num8 < array.Length)
												{
													if (array[num8] == num7)
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
												AIItemData aiitemData3 = this.decisionResult[num7].Element as AIItemData;
												num -= this.decisionResult[num7].Score;
												for (int num9 = 0; num9 < aiitemData3.ItemDefinition.Costs.Length; num9++)
												{
													for (int num10 = 0; num10 < this.currentResources.Length; num10++)
													{
														if (aiitemData3.ItemDefinition.Costs[num9].ResourceName == this.resourcePolicy[num10].Name)
														{
															this.currentResources[num10] -= aiitemData3.ItemDefinition.Costs[num9].GetValue(base.AIEntity.Empire);
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
											}
										}
										num += this.decisionResult[k].Score;
										DepartmentOfDefense.ApplyEquipmentSet(unit);
										for (int num11 = 0; num11 < aiitemData.ItemDefinition.Costs.Length; num11++)
										{
											for (int num12 = 0; num12 < this.currentResources.Length; num12++)
											{
												if (aiitemData.ItemDefinition.Costs[num11].ResourceName == this.resourcePolicy[num12].Name)
												{
													this.currentResources[num12] += aiitemData.ItemDefinition.Costs[num11].GetValue(base.AIEntity.Empire);
													break;
												}
											}
										}
									}
								}
								IL_815:;
							}
						}
					}
				}
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
		for (int i = 0; i < currentItemData.ItemDefinition.Costs.Length; i++)
		{
			int j = 0;
			while (j < this.currentResources.Length)
			{
				if (currentItemData.ItemDefinition.Costs[i].ResourceName == this.resourcePolicy[j].Name)
				{
					float value = currentItemData.ItemDefinition.Costs[i].GetValue(base.AIEntity.Empire);
					if (value + this.currentResources[j] >= this.resourcePolicy[j].Value)
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
		this.unitAssignationParameterModifier.Clear();
		float num = this.ComputeUnitDesignScore(originalUnitDesign);
		UnitDesign unitDesign;
		float num2 = this.GenerateNewUnitDesign(originalUnitDesign, out unitDesign);
		if (num < num2 * this.newUnitDesignScoreModifier)
		{
			bool flag = false;
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
		chosenSlotIndex = -1;
		bool flag = false;
		for (int i = 0; i < itemDefinition.Slots.Length; i++)
		{
			for (int j = 0; j < itemDefinition.Slots[i].SlotTypes.Length; j++)
			{
				int num = this.FindFirstEmptySlotIndex(itemDataBySlot, unitEquipmentSet, itemDefinition.Slots[i].SlotTypes[j], unit);
				if (num < 0)
				{
					return false;
				}
				if (itemDataBySlot[num] < 0)
				{
					flag = true;
				}
			}
			if (flag)
			{
				chosenSlotIndex = i;
				break;
			}
		}
		return flag;
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
				int index;
				for (index = 0; index < this.departmentOfEducation.Heroes.Count; index++)
				{
					if (this.departmentOfEducation.Heroes[index].UnitDesign.Model == orderEditHeroUnitDesign.UnitDesignModel)
					{
						EvaluableMessage_RetrofitUnit firstMessage = base.AIEntity.AIPlayer.Blackboard.GetFirstMessage<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire, (EvaluableMessage_RetrofitUnit match) => match.ElementGuid == this.departmentOfEducation.Heroes[index].GUID);
						if (firstMessage != null)
						{
							if (this.currentEditHeroOrderTicket.PostOrderResponse == PostOrderResponse.Processed)
							{
								firstMessage.SetObtained();
							}
							else
							{
								firstMessage.SetFailedToObtain();
							}
						}
						break;
					}
				}
			}
			this.currentEditHeroOrderTicket = null;
		}
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		foreach (EvaluableMessage_RetrofitUnit evaluableMessage_RetrofitUnit in base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_RetrofitUnit>(BlackboardLayerID.Empire))
		{
			AIData_Unit aidata_Unit;
			if (evaluableMessage_RetrofitUnit.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && this.aiDataRepositoryHelper.TryGetAIData<AIData_Unit>(evaluableMessage_RetrofitUnit.ElementGuid, out aidata_Unit) && aidata_Unit.Unit.UnitDesign is UnitProfile)
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
					evaluableMessage_RetrofitUnit.SetFailedToObtain();
				}
				else
				{
					DepartmentOfDefense.CheckRetrofitPrerequisitesResult checkRetrofitPrerequisitesResult = agency.CheckRetrofitPrerequisites(aidata_Unit.Unit, agency.GetRetrofitCosts(aidata_Unit.Unit, unitDesign));
					if (checkRetrofitPrerequisitesResult == DepartmentOfDefense.CheckRetrofitPrerequisitesResult.Ok)
					{
						OrderEditHeroUnitDesign order = new OrderEditHeroUnitDesign(base.AIEntity.Empire.Index, unitDesign);
						base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out this.currentEditHeroOrderTicket, null);
						return SynchronousJobState.Running;
					}
					evaluableMessage_RetrofitUnit.SetFailedToObtain();
				}
			}
		}
		return SynchronousJobState.Success;
	}

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private IAIEmpireDataAIHelper empireDataHelper;

	private List<AIParameter.AIModifier> unitSlayerModifier;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	public static string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_UnitDesigner";

	private AfterBattleAnalyzer afterBattleAnalyzer;

	private IDatabase<AfterBattleDefinition> afterBattleDatabase;

	private List<AfterBattleData> afterBattleDataByBody = new List<AfterBattleData>();

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AILayer_Encounter aiLayerEncounter;

	private List<AIItemData> availableItemData = new List<AIItemData>();

	private Dictionary<StaticString, string> bestTierByResourceName = new Dictionary<StaticString, string>();

	private float costModifier = 0.5f;

	private AfterBattleData currentAfterBattleData;

	private bool isCurrentDesignSeafaring;

	private Ticket currentEditHeroOrderTicket;

	private float[] currentResources;

	private List<DecisionResult> decisionResult = new List<DecisionResult>();

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfEducation departmentOfEducation;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private AfterBattleData empireModifiers;

	private IEndTurnService endTurnService;

	private global::Game game;

	private List<UnitDesign> heroDesignToImprove = new List<UnitDesign>();

	private SimulationDecisionMaker<AIItemData> itemDataDecisionMaker;

	private IAIItemDataRepository itemDataRepository;

	private AIParameter.AIModifier[] modifierBoost;

	private AIParameter.AIModifier[] modifierMaximalValue;

	private AIParameter.AIModifier[] modifierNavalMaximalValue;

	private List<AIParameter.AIModifier> unitAssignationParameterModifier = new List<AIParameter.AIModifier>();

	private IPersonalityAIHelper personalityHelper;

	private List<AIScoring> resourcePolicy;

	private float scoreLimit = 0.05f;

	private ISynchronousJobRepositoryAIHelper synchronousJobRepositoryHelper;

	private IAIUnitDesignDataRepository unitDesignRepository;

	private List<UnitDesign> unitDesignToImprove = new List<UnitDesign>();

	private float newUnitDesignScoreModifier = 0.8f;
}
