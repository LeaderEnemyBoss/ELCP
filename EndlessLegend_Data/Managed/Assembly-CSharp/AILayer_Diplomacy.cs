using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amplitude;
using Amplitude.Collections;
using Amplitude.Math;
using Amplitude.Unity.AI;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Decision;
using Amplitude.Unity.AI.Evaluation;
using Amplitude.Unity.AI.Evaluation.Diagnostics;
using Amplitude.Unity.Event;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Xml;
using Amplitude.Xml;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy/", new object[]
{

})]
public class AILayer_Diplomacy : AILayer, ITickable, IUpdatable, ISimulationAIEvaluationHelper<DiplomaticTerm>, IAIEvaluationHelper<DiplomaticTerm, InterpreterContext>
{
	public AILayer_Diplomacy()
	{
		this.aiParametersByElement = new Dictionary<StaticString, IAIParameter<InterpreterContext>[]>();
		this.ContractRequests = new List<AILayer_Diplomacy.ContractRequest>();
		this.minimumNumberOfTurnsBetweenPropositions = 5;
		this.maximumNumberOfTurnsBetweenPropositions = 10;
		this.maximumNumberOfTurnsBetweenStatusAndForceStatus = 20;
		this.DebugEvaluationsHistoric = new FixedSizedList<EvaluationData<DiplomaticTerm, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);
		this.availableTermEvaluationsCacheList = new List<AILayer_Diplomacy.TermEvaluation>();
		this.diplomaticTermsCacheList = new List<DiplomaticTerm>();
		this.diplomaticRelationStateAgentCriticityThreshold = 0.5f;
		this.diplomaticTermAgentCriticityThreshold = 0.5f;
		this.diplomaticRelationStateAgentCriticityEpsilon = 0.05f;
		this.maximumContractPropositionEvaluationScore = 10f;
		this.maximumExchangeAmountRatio = 0.8f;
		this.minimumExchangeAmountRatio = 0.2f;
		this.diplomaticMaximumAccountMultiplier = 25f;
		this.diplomaticRelationStateByAmasAgentName = new Dictionary<StaticString, DiplomaticRelationState>();
		this.diplomaticTermByAmasAgentName = new Dictionary<StaticString, DiplomaticTermDefinition[]>();
		this.proposalTermByAmasAgentName = new Dictionary<StaticString, DiplomaticTermDefinition[]>();
		this.wantedDiplomaticRelationStateMessageRevaluationPeriod = 4;
		this.decisions = new List<DecisionResult>();
		this.NeedsVictoryReaction = new bool[ELCPUtilities.NumberOfMajorEmpires];
		this.PeaceWish = new bool[ELCPUtilities.NumberOfMajorEmpires];
		this.DealIndeces = new List<int>();
		this.PrisonDealIndeces = new List<int>();
		this.MimicCityDealIndeces = new List<int>();
		this.currentDiplomaticTerms = new List<DiplomaticTerm>();
	}

	public float AnalyseContractProposition(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		Diagnostics.Assert(this.empire != null && (this.empire.Index == diplomaticContract.EmpireWhichReceives.Index || this.empire.Index == diplomaticContract.EmpireWhichProposes.Index));
		global::Empire otherEmpire = (diplomaticContract.EmpireWhichProposes.Index != this.empire.Index) ? diplomaticContract.EmpireWhichProposes : diplomaticContract.EmpireWhichReceives;
		return this.AnalyseContractProposition(diplomaticContract.Terms, otherEmpire);
	}

	private float AnalyseContractProposition(IEnumerable<DiplomaticTerm> diplomaticTerms, global::Empire otherEmpire)
	{
		this.decisions.Clear();
		this.EvaluateDecisions(diplomaticTerms, otherEmpire, ref this.decisions);
		float num = 0f;
		if (this.decisions != null)
		{
			for (int i = 0; i < this.decisions.Count; i++)
			{
				num += this.decisions[i].Score;
			}
		}
		return num;
	}

	private void AnswerContractProposition(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration)
		{
			return;
		}
		Diagnostics.Assert(diplomaticContract.EmpireWhichReceives != null && diplomaticContract.EmpireWhichProposes != null && this.empire != null);
		Diagnostics.Assert(diplomaticContract.EmpireWhichReceives.Index == this.empire.Index);
		Diagnostics.Assert(diplomaticContract.EmpireWhichProposes.Index != this.empire.Index);
		if (diplomaticContract.IsTransitionPossible(DiplomaticContractState.Ignored))
		{
			this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Ignored);
		}
		if (!diplomaticContract.IsTransitionPossible(DiplomaticContractState.Signed))
		{
			this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Refused);
			return;
		}
		this.CurrentAnswerContract = diplomaticContract;
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Enter(this.CurrentAnswerContract);
		}
		diplomaticContract.EmpireWhichReceives.Refresh(true);
		diplomaticContract.EmpireWhichProposes.Refresh(true);
		float num = this.AnalyseContractProposition(diplomaticContract);
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Exit(this.CurrentAnswerContract);
		}
		this.CurrentAnswerContract = null;
		if (num >= 0f)
		{
			this.aiLayerAttitude.RegisterContractBenefitForMyEmpire(diplomaticContract, num);
			this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Signed);
			return;
		}
		this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Refused);
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

	public IEnumerable<IAIParameter<InterpreterContext>> GetAIParameters(DiplomaticTerm element)
	{
		if (element == null)
		{
			throw new ArgumentNullException("element");
		}
		Diagnostics.Assert(element.Definition != null);
		StaticString key = this.GetKeyFromDiplomaticTerm(element);
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(key));
		StaticString finalKey = string.Format("{0}{1}", key, (element.EmpireWhichProvides.Index != this.empire.Index) ? "Receiver" : "Provider");
		Diagnostics.Assert(this.aiParametersByElement != null);
		if (!this.aiParametersByElement.ContainsKey(finalKey))
		{
			finalKey = key;
			if (!this.aiParametersByElement.ContainsKey(finalKey))
			{
				yield break;
			}
		}
		IAIParameter<InterpreterContext>[] aiParameters = this.aiParametersByElement[finalKey];
		Diagnostics.Assert(aiParameters != null);
		for (int index = 0; index < aiParameters.Length; index++)
		{
			yield return aiParameters[index];
		}
		yield break;
	}

	public IEnumerable<IAIPrerequisite<InterpreterContext>> GetAIPrerequisites(DiplomaticTerm element)
	{
		yield break;
	}

	private StaticString GetKeyFromDiplomaticTerm(DiplomaticTerm diplomaticTerm)
	{
		StaticString result = diplomaticTerm.Definition.Name;
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm as DiplomaticTermResourceExchange;
		if (diplomaticTermResourceExchange != null)
		{
			Diagnostics.Assert(diplomaticTermResourceExchange.Definition != null);
			result = string.Format("ResourceExchange{0}", diplomaticTermResourceExchange.ResourceName);
		}
		DiplomaticTermCityExchange diplomaticTermCityExchange = diplomaticTerm as DiplomaticTermCityExchange;
		if (diplomaticTermCityExchange != null)
		{
			result = string.Format("CityExchange", new object[0]);
		}
		DiplomaticTermFortressExchange diplomaticTermFortressExchange = diplomaticTerm as DiplomaticTermFortressExchange;
		if (diplomaticTermFortressExchange != null)
		{
			result = string.Format("FortressExchange", new object[0]);
		}
		DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm as DiplomaticTermBoosterExchange;
		if (diplomaticTermBoosterExchange != null)
		{
			Diagnostics.Assert(diplomaticTermBoosterExchange.Definition != null);
			result = string.Format("BoosterExchange{0}", diplomaticTermBoosterExchange.BoosterDefinitionName);
		}
		DiplomaticTermProposalDefinition diplomaticTermProposalDefinition = diplomaticTerm.Definition as DiplomaticTermProposalDefinition;
		if (diplomaticTermProposalDefinition != null && !StaticString.IsNullOrEmpty(diplomaticTermProposalDefinition.AIParametersName))
		{
			result = diplomaticTermProposalDefinition.AIParametersName;
		}
		return result;
	}

	private IEnumerable<StaticString> GetKeysFromDiplomaticTermDefinition(DiplomaticTermDefinition definition)
	{
		DiplomaticTermResourceExchangeDefinition diplomaticTermResourceExchangeDefinition = definition as DiplomaticTermResourceExchangeDefinition;
		if (diplomaticTermResourceExchangeDefinition != null)
		{
			if (diplomaticTermResourceExchangeDefinition.TradableResources != null && diplomaticTermResourceExchangeDefinition.TradableResources.TradableResourceReferences != null)
			{
				for (int resourceIndex = 0; resourceIndex < diplomaticTermResourceExchangeDefinition.TradableResources.TradableResourceReferences.Length; resourceIndex++)
				{
					XmlNamedReference tradableResourceReference = diplomaticTermResourceExchangeDefinition.TradableResources.TradableResourceReferences[resourceIndex];
					yield return string.Format("ResourceExchange{0}", tradableResourceReference);
				}
			}
			yield break;
		}
		DiplomaticTermBoosterExchangeDefinition diplomaticTermBoosterExchangeDefinition = definition as DiplomaticTermBoosterExchangeDefinition;
		if (diplomaticTermBoosterExchangeDefinition != null)
		{
			if (diplomaticTermBoosterExchangeDefinition.Boosters != null && diplomaticTermBoosterExchangeDefinition.Boosters.BoosterReferences != null)
			{
				for (int resourceIndex2 = 0; resourceIndex2 < diplomaticTermBoosterExchangeDefinition.Boosters.BoosterReferences.Length; resourceIndex2++)
				{
					XmlNamedReference boosterReference = diplomaticTermBoosterExchangeDefinition.Boosters.BoosterReferences[resourceIndex2];
					yield return string.Format("BoosterExchange{0}", boosterReference);
				}
			}
			yield break;
		}
		DiplomaticTermCityExchangeDefinition diplomaticTermCityExchange = definition as DiplomaticTermCityExchangeDefinition;
		if (diplomaticTermCityExchange != null)
		{
			yield return "CityExchange";
			yield break;
		}
		DiplomaticTermFortressExchangeDefinition diplomaticTermFortressExchange = definition as DiplomaticTermFortressExchangeDefinition;
		if (diplomaticTermFortressExchange != null)
		{
			yield return "FortressExchange";
			yield break;
		}
		DiplomaticTermProposalDefinition proposalDefinition = definition as DiplomaticTermProposalDefinition;
		if (proposalDefinition != null && !StaticString.IsNullOrEmpty(proposalDefinition.AIParametersName))
		{
			yield return proposalDefinition.AIParametersName;
			yield break;
		}
		yield return definition.Name;
		yield break;
	}

	private void InitializeAIEvaluableElement(DiplomaticTermDefinition diplomaticTermDefinition, StaticString aiParametersReference)
	{
		if (aiParametersReference == null)
		{
			throw new ArgumentNullException("aiParametersReference");
		}
		Diagnostics.Assert(!StaticString.IsNullOrEmpty(aiParametersReference));
		Diagnostics.Assert(this.aiParametersByElement != null);
		if (this.aiParametersByElement.ContainsKey(aiParametersReference))
		{
			return;
		}
		List<IAIParameter<InterpreterContext>> list = new List<IAIParameter<InterpreterContext>>();
		AIInfo aiinfo = diplomaticTermDefinition.AIInfo;
		if (aiinfo != null && aiinfo.AIParameters != null)
		{
			list.AddRange(aiinfo.AIParameters);
		}
		else if (this.aiParameterDatabase != null && this.aiParameterDatabase.ContainsKey(aiParametersReference))
		{
			AIParameterDatatableElement value = this.aiParameterDatabase.GetValue(aiParametersReference);
			if (value == null)
			{
				AILayer.LogWarning("Cannot retrieve ai parameters for constructible element '{0}'", new object[]
				{
					aiParametersReference
				});
				return;
			}
			if (value.AIParameters == null)
			{
				AILayer.LogWarning("There aren't any parameters in aiParameters '{0}'", new object[]
				{
					aiParametersReference
				});
				return;
			}
			for (int i = 0; i < value.AIParameters.Length; i++)
			{
				AIParameterDatatableElement.AIParameter aiparameter = value.AIParameters[i];
				Diagnostics.Assert(aiparameter != null);
				list.Add(aiparameter.Instantiate());
			}
		}
		if (list.Count > 0)
		{
			this.aiParametersByElement.Add(aiParametersReference, list.ToArray());
		}
	}

	private void InitializeDiplomaticTermEvaluationHelper()
	{
		Diagnostics.Assert(this.aiParametersByElement != null);
		this.aiParametersByElement.Clear();
		Diagnostics.Assert(this.contructibleElementDatabase != null);
		DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
		if (values != null)
		{
			for (int i = 0; i < values.Length; i++)
			{
				DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
				if (diplomaticTermDefinition != null)
				{
					foreach (StaticString staticString in this.GetKeysFromDiplomaticTermDefinition(diplomaticTermDefinition))
					{
						this.InitializeAIEvaluableElement(diplomaticTermDefinition, staticString);
						this.InitializeAIEvaluableElement(diplomaticTermDefinition, string.Format("{0}Provider", staticString));
						this.InitializeAIEvaluableElement(diplomaticTermDefinition, string.Format("{0}Receiver", staticString));
					}
				}
			}
		}
	}

	private void RelationStateChanged(DiplomaticRelationStateChangeEventArgs e)
	{
		if (e.DiplomaticRelationState.Name == DiplomaticRelationState.Names.War && e.PreviousDiplomaticRelationState.Name != DiplomaticRelationState.Names.War)
		{
			this.RelationStateChangeToWar(e.EmpireWithWhichTheStatusChange);
		}
		if (e.DiplomaticRelationState.Name == DiplomaticRelationState.Names.Truce || e.DiplomaticRelationState.Name == DiplomaticRelationState.Names.Dead)
		{
			this.RelationStateChangeToTruce(e.EmpireWithWhichTheStatusChange);
		}
	}

	private void RelationStateChangeToWar(global::Empire enemyEmpire)
	{
		if (!this.IsActive())
		{
			return;
		}
		this.GetMilitaryPowerDif(true);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		bool flag = this.empire.GetPropertyValue(SimulationProperties.MilitaryPower) < enemyEmpire.GetPropertyValue(SimulationProperties.MilitaryPower) || this.empire.GetPropertyValue(SimulationProperties.WarCount) > 1f;
		float propertyValue = enemyEmpire.GetPropertyValue(SimulationProperties.MilitaryPower);
		int num = 0;
		List<MajorEmpire> list = new List<MajorEmpire>();
		List<MajorEmpire> list2 = new List<MajorEmpire>();
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = this.game.Empires[i] as MajorEmpire;
			if (majorEmpire != null && majorEmpire.Index != enemyEmpire.Index && this.empire.Index != majorEmpire.Index && !majorEmpire.IsEliminated && !majorEmpire.SimulationObject.Tags.Contains(AILayer_War.TagNoWarTrait) && (this.GetAgentCriticityFor(majorEmpire, AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar) > 0.5f || flag))
			{
				DepartmentOfForeignAffairs agency = majorEmpire.GetAgency<DepartmentOfForeignAffairs>();
				if (!this.AlreadyContactedEmpires.Contains(majorEmpire.Index) && majorEmpire.GetPropertyValue(SimulationProperties.MilitaryPower) * 5f > propertyValue && this.departmentOfForeignAffairs.DiplomaticRelations[i].State != null && agency.DiplomaticRelations[enemyEmpire.Index].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name != DiplomaticRelationState.Names.War && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name != DiplomaticRelationState.Names.Dead && agency.DiplomaticRelations[enemyEmpire.Index].State.Name != DiplomaticRelationState.Names.War && agency.DiplomaticRelations[enemyEmpire.Index].State.Name != DiplomaticRelationState.Names.Unknown && agency.DiplomaticRelations[enemyEmpire.Index].State.Name != DiplomaticRelationState.Names.Alliance)
				{
					float num2 = 0f;
					Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == majorEmpire.Index) || (contract.EmpireWhichProposes.Index == majorEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
					foreach (DiplomaticContract diplomaticContract in this.diplomacyContractRepositoryService.FindAll(match))
					{
						if (diplomaticContract.EmpireWhichInitiated.Index == this.empire.Index)
						{
							num2 = Mathf.Max(num2, (float)diplomaticContract.TurnAtTheBeginningOfTheState);
						}
					}
					if ((float)this.game.Turn - num2 > 0f)
					{
						if (service.GetCommonBorderRatio(majorEmpire, enemyEmpire) > 0f)
						{
							list.Add(majorEmpire);
						}
						else
						{
							list2.Add(majorEmpire);
						}
					}
				}
			}
		}
		list.AddRange(list2);
		foreach (MajorEmpire majorEmpire2 in list)
		{
			AILayer_Diplomacy ailayer_Diplomacy = null;
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (this.aIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire2, out aiplayer_MajorEmpire))
			{
				AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
				if (entity != null)
				{
					ailayer_Diplomacy = entity.GetLayer<AILayer_Diplomacy>();
				}
			}
			if (majorEmpire2.GetPropertyValue(SimulationProperties.WarCount) < 1f || (majorEmpire2.GetPropertyValue(SimulationProperties.WarCount) < 2f && ailayer_Diplomacy != null && ailayer_Diplomacy.GetMilitaryPowerDif(true) > majorEmpire2.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 0.2f))
			{
				if (this.departmentOfForeignAffairs.DiplomaticRelations[majorEmpire2.Index].State.Name == DiplomaticRelationState.Names.ColdWar)
				{
					this.AlwaysProcess = true;
					int count = this.ContractRequests.Count;
					this.TryGenerateDiplomaticStateChangeContractRequest(majorEmpire2, DiplomaticRelationState.Names.Peace);
					this.AlwaysProcess = false;
					if (this.ContractRequests.Count > count)
					{
						num++;
						this.AlreadyContactedEmpires.Add(majorEmpire2.Index);
					}
				}
				else
				{
					StaticString askToDeclareWar = AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar;
					DiplomaticTermProposal item;
					if (AILayer_Diplomacy.VictoryTargets[majorEmpire2.Index] < 0 && this.TryGenerateAskToDiplomaticTerm(majorEmpire2, enemyEmpire, askToDeclareWar, out item))
					{
						AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, majorEmpire2);
						contractRequest.Terms.Add(item);
						contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
						this.ContractRequests.Add(contractRequest);
						this.AlreadyContactedEmpires.Add(majorEmpire2.Index);
						num++;
					}
				}
			}
			if ((float)num >= this.EmergencyGangupLimit)
			{
				break;
			}
		}
		if (num > 0)
		{
			this.State = TickableState.NeedTick;
		}
	}

	private void ClearContractRequests()
	{
		for (int i = 0; i < this.ContractRequests.Count; i++)
		{
			AILayer_Diplomacy.ContractRequest contractRequest = this.ContractRequests[i];
			if (contractRequest.State != AILayer_Diplomacy.ContractRequest.ContractRequestState.Done)
			{
				AILayer.LogWarning("Contract request failed: {0}", new object[]
				{
					contractRequest
				});
			}
		}
		this.ContractRequests.Clear();
	}

	private void GenerateAskToContractRequest(MajorEmpire opponentEmpire)
	{
		int num = int.MinValue;
		int num2 = int.MinValue;
		Predicate<DiplomaticContract> match = (DiplomaticContract contract) => contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index;
		Func<DiplomaticTerm, bool> <>9__1;
		foreach (DiplomaticContract diplomaticContract in this.diplomacyContractRepositoryService.FindAll(match))
		{
			if (diplomaticContract.State == DiplomaticContractState.Refused || diplomaticContract.State == DiplomaticContractState.Ignored)
			{
				IEnumerable<DiplomaticTerm> terms = diplomaticContract.Terms;
				Func<DiplomaticTerm, bool> predicate;
				if ((predicate = <>9__1) == null)
				{
					predicate = (<>9__1 = ((DiplomaticTerm t) => t is DiplomaticTermProposal && t.EmpireWhichReceives.Index == this.empire.Index));
				}
				DiplomaticTerm diplomaticTerm = terms.FirstOrDefault(predicate);
				if (diplomaticTerm != null)
				{
					if (diplomaticTerm.Definition.Name == DiplomaticTermDefinition.Names.AskToDeclareWar)
					{
						if (diplomaticContract.TurnAtTheBeginningOfTheState > num)
						{
							num = diplomaticContract.TurnAtTheBeginningOfTheState;
						}
					}
					else if (diplomaticContract.TurnAtTheBeginningOfTheState > num2)
					{
						num2 = diplomaticContract.TurnAtTheBeginningOfTheState;
					}
				}
			}
		}
		if (num > 0 && this.game.Turn - num > 15)
		{
			this.GenerateAskToDeclareWarContractRequest(opponentEmpire);
		}
		if (num2 > 0 && this.game.Turn - num2 > 15)
		{
			this.GenerateAskToBlackSpotContractRequest(opponentEmpire);
		}
	}

	private void GenerateAskToDeclareWarContractRequest(MajorEmpire opponentEmpire)
	{
		StaticString askToDeclareWar = AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar;
		if (this.GetAgentCriticityFor(opponentEmpire, askToDeclareWar) > 0.5f || this.AlwaysProcess || (this.MilitaryPowerDif < 0f && Mathf.Abs(this.MilitaryPowerDif) > this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 0.5f))
		{
			List<int> list = new List<int>();
			DepartmentOfForeignAffairs agency = opponentEmpire.GetAgency<DepartmentOfForeignAffairs>();
			int k;
			int i;
			for (i = 0; i < this.departmentOfForeignAffairs.DiplomaticRelations.Count; i = k + 1)
			{
				if (this.departmentOfForeignAffairs.DiplomaticRelations[i].State != null && agency.DiplomaticRelations[i].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.War && agency.DiplomaticRelations[i].State.Name != DiplomaticRelationState.Names.War && opponentEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 5f > this.game.Empires[i].GetPropertyValue(SimulationProperties.LandMilitaryPower) && this.game.Empires[i].GetPropertyValue(SimulationProperties.LandMilitaryPower) * 3f > this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) && (this.ContractRequests.Count == 0 || this.AlwaysProcess || !this.ContractRequests.Any((AILayer_Diplomacy.ContractRequest c) => c.OpponentEmpire.Index == i)))
				{
					list.Add(i);
				}
				k = i;
			}
			if (list.Count > 0)
			{
				int num = -1;
				float num2 = 0f;
				for (int j = 0; j < list.Count; j++)
				{
					DiplomaticTermProposal term;
					if (this.TryGenerateAskToDiplomaticTerm(opponentEmpire, this.game.Empires[list[j]], askToDeclareWar, out term))
					{
						DecisionResult decisionResult = this.EvaluateDecision(term, opponentEmpire, null);
						if (decisionResult.Score > num2)
						{
							num2 = decisionResult.Score;
							num = list[j];
						}
					}
				}
				if (num >= 0)
				{
					AILayer_Diplomacy.ContractRequest contractRequest = this.GenerateContractRequest_AskTo(opponentEmpire, this.game.Empires[num], askToDeclareWar);
					if (contractRequest != null)
					{
						contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeScored;
						if (this.AlwaysProcess)
						{
							contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
						}
						this.ContractRequests.Add(contractRequest);
					}
				}
			}
		}
	}

	private void GenerateAskToBlackSpotContractRequest(MajorEmpire opponentEmpire)
	{
		StaticString askToBlackSpotTermAgent = AILayer_DiplomacyAmas.AgentNames.AskToBlackSpotTermAgent;
		if (this.GetAgentCriticityFor(opponentEmpire, askToBlackSpotTermAgent) > 0.5f || (this.departmentOfForeignAffairs.IsInWarWithSomeone() && this.MilitaryPowerDif < 0f))
		{
			int num = -1;
			float num2 = 0f;
			DepartmentOfForeignAffairs agency = opponentEmpire.GetAgency<DepartmentOfForeignAffairs>();
			for (int i = 0; i < this.game.Empires.Length; i++)
			{
				MajorEmpire majorEmpire = this.game.Empires[i] as MajorEmpire;
				if (majorEmpire != null && majorEmpire.Index != this.empire.Index && majorEmpire.Index != opponentEmpire.Index && (agency.DiplomaticRelations[i].State == null || (!(agency.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.Peace) && !(agency.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.Alliance))) && this.GetGlobalScore(majorEmpire) > num2)
				{
					num = majorEmpire.Index;
				}
			}
			DiplomaticTermProposal term;
			if (num > 0 && this.TryGenerateAskToDiplomaticTerm(opponentEmpire, this.game.Empires[num], askToBlackSpotTermAgent, out term) && this.EvaluateDecision(term, opponentEmpire, null).Score > 0f)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = this.GenerateContractRequest_AskTo(opponentEmpire, this.game.Empires[num], askToBlackSpotTermAgent);
				if (contractRequest != null)
				{
					contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeScored;
					this.ContractRequests.Add(contractRequest);
				}
			}
		}
	}

	private AILayer_Diplomacy.ContractRequest GenerateContractRequest_AskTo(global::Empire alliedEmpire, global::Empire commonEnemy, StaticString proposalTermName)
	{
		DiplomaticTermProposal item = null;
		if (!this.TryGenerateAskToDiplomaticTerm(alliedEmpire, commonEnemy, proposalTermName, out item))
		{
			return null;
		}
		return new AILayer_Diplomacy.ContractRequest(this.empire, alliedEmpire)
		{
			Terms = 
			{
				item
			}
		};
	}

	private void GenerateContractRequests(MajorEmpire opponentEmpire)
	{
		if (opponentEmpire == null)
		{
			throw new ArgumentNullException("opponentEmpire");
		}
		opponentEmpire.Refresh(true);
		Blackboard<BlackboardLayerID, BlackboardMessage> blackboard = base.AIEntity.AIPlayer.Blackboard;
		int num;
		int num2;
		int turnsSinceLastDeal;
		int turnsSinceLastPrisonerDeal;
		int turnsSinceLastMimicsCityDeal;
		int turnsSinceLastDeclarationByMe;
		this.GenerateContractRequests_ComputeLastContractTurns(opponentEmpire, out num, out num2, out turnsSinceLastDeal, out turnsSinceLastPrisonerDeal, out turnsSinceLastMimicsCityDeal, out turnsSinceLastDeclarationByMe);
		int num3 = this.game.Turn - num;
		int num4 = this.minimumNumberOfTurnsBetweenPropositions + num2 * (base.AIEntity.Empire.Index + 1) % (this.maximumNumberOfTurnsBetweenPropositions - this.minimumNumberOfTurnsBetweenPropositions + 1);
		if (this.DiplomacyFocus && num4 > 3)
		{
			num4--;
		}
		WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == opponentEmpire.Index);
		if (num3 < num4)
		{
			this.GenerateContractRequests_TryImmediateActions(opponentEmpire, wantedDiplomaticRelationStateMessage, num3);
			return;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		if (!this.GenerateContractRequests_TryVictoryReactions(opponentEmpire, wantedDiplomaticRelationStateMessage, diplomaticRelation) && !this.GenerateContractRequests_TryOpportunistWarDeclaration(opponentEmpire, wantedDiplomaticRelationStateMessage, diplomaticRelation))
		{
			this.GenerateContractRequests_TryStandardRelationstateChange(opponentEmpire, wantedDiplomaticRelationStateMessage, diplomaticRelation);
			if (this.ContractRequests.Count == 0)
			{
				this.GenerateAskToContractRequest(opponentEmpire);
			}
			this.GenerateContractRequests_TryForceTruce(opponentEmpire, turnsSinceLastDeclarationByMe);
			StaticString staticString = this.mostWantedDiplomaticTermAgentNameByEmpireIndex[opponentEmpire.Index];
			if (!StaticString.IsNullOrEmpty(staticString))
			{
				this.GenerateTreatyContractRequest(opponentEmpire, staticString);
			}
			this.GenerateContractRequests_TryELCPDeals(opponentEmpire, diplomaticRelation, turnsSinceLastDeal, turnsSinceLastMimicsCityDeal, turnsSinceLastPrisonerDeal);
		}
	}

	private void GenerateDiscussionContractRequest(MajorEmpire opponentEmpire, DiplomaticTermAlignment alignement)
	{
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		Diagnostics.Assert(diplomaticRelation != null);
		if (diplomaticRelation.DiscussionChaosScore > 0f)
		{
			AILayer.Log("GenerateDiscussionContractRequest failed because of non nul discussion chaos {0} between empire {1} and empire {2}.", new object[]
			{
				diplomaticRelation.DiscussionChaosScore,
				this.empire.Index,
				opponentEmpire.Index
			});
			return;
		}
		DiplomaticTerm diplomaticTerm = null;
		if (alignement == DiplomaticTermAlignment.Bad)
		{
			diplomaticTerm = new DiplomaticTerm(this.diplomaticTermWarning, this.empire, this.empire, opponentEmpire);
		}
		else if (alignement == DiplomaticTermAlignment.Good)
		{
			diplomaticTerm = new DiplomaticTerm(this.diplomaticTermGratify, this.empire, this.empire, opponentEmpire);
		}
		if (diplomaticTerm == null)
		{
			return;
		}
		float num;
		float num2;
		if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, out num2))
		{
			AILayer.LogError("Can't retrieve empire point account infos");
		}
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
		if (empirePointCost > num)
		{
			return;
		}
		AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
		contractRequest.Terms.Add(diplomaticTerm);
		this.ContractRequests.Add(contractRequest);
	}

	private void GenerateTreatyContractRequest(MajorEmpire opponentEmpire, StaticString mostWantedDiplomaticTermAgentName)
	{
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		DiplomaticTermDefinition diplomaticTermDefinition = null;
		foreach (DiplomaticTermDefinition diplomaticTermDefinition2 in this.diplomaticTermByAmasAgentName[mostWantedDiplomaticTermAgentName])
		{
			if (DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, this.empire, opponentEmpire, diplomaticTermDefinition2, new string[0]))
			{
				diplomaticTermDefinition = diplomaticTermDefinition2;
				break;
			}
		}
		if (diplomaticTermDefinition != null)
		{
			List<DiplomaticTerm> list = new List<DiplomaticTerm>();
			if (diplomaticTermDefinition is DiplomaticTermProposalDefinition)
			{
				DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, opponentEmpire, this.empire, ref list);
			}
			else
			{
				DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, opponentEmpire, ref list);
			}
			Diagnostics.Assert(list != null);
			float num;
			float num2;
			if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, out num2))
			{
				AILayer.LogError("Can't retrieve empire point account infos");
			}
			this.aiLayerAccountManager.TryGetAccount(AILayer_AccountManager.DiplomacyAccountName);
			float num3;
			this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num3, false);
			for (int j = 0; j < list.Count; j++)
			{
				DiplomaticTerm diplomaticTerm = list[j];
				float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
				if (empirePointCost <= num || (this.DiplomacyFocus && empirePointCost * 5f < num3))
				{
					AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
					Diagnostics.Assert(contractRequest.Terms != null);
					contractRequest.Terms.Add(diplomaticTerm);
					this.ContractRequests.Add(contractRequest);
					return;
				}
			}
		}
	}

	private bool TryGenerateAskToDiplomaticTerm(global::Empire alliedEmpire, global::Empire commonEnemy, StaticString proposalTermName, out DiplomaticTermProposal proposal)
	{
		proposal = null;
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, alliedEmpire);
		DiplomaticTermProposalDefinition diplomaticTermProposalDefinition = null;
		for (int i = 0; i < this.proposalTermByAmasAgentName[proposalTermName].Length; i++)
		{
			if (DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, this.empire, alliedEmpire, this.proposalTermByAmasAgentName[proposalTermName][i], new string[0]))
			{
				diplomaticTermProposalDefinition = (this.proposalTermByAmasAgentName[proposalTermName][i] as DiplomaticTermProposalDefinition);
				break;
			}
		}
		if (diplomaticTermProposalDefinition == null)
		{
			return false;
		}
		proposal = new DiplomaticTermProposal(diplomaticTermProposalDefinition, this.empire, alliedEmpire, this.empire);
		proposal.ChangeEmpire(diplomaticContract, commonEnemy);
		if (!proposal.CanApply(diplomaticContract, new string[0]))
		{
			return false;
		}
		float num;
		float num2;
		if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, out num2))
		{
			AILayer.LogError("Can't retrieve empire point account infos");
		}
		float num3 = 0f;
		base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num3, false);
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(proposal, this.empire);
		return empirePointCost <= num || empirePointCost * 3f < num3;
	}

	private void TryGenerateDiplomaticStateChangeContractRequest(MajorEmpire opponentEmpire, StaticString wantedDiplomaticRelationState)
	{
		if (wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
		{
			this.opportunistThisTurn = true;
		}
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
		StaticString name = diplomaticRelation.State.Name;
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = null;
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition2 = null;
		foreach (DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition3 in this.departmentOfForeignAffairs.GetDiplomaticTermDiplomaticRelationStateDefinition(diplomaticContract, wantedDiplomaticRelationState))
		{
			Diagnostics.Assert(diplomaticTermDiplomaticRelationStateDefinition3 != null);
			if (diplomaticTermDiplomaticRelationStateDefinition == null || diplomaticTermDiplomaticRelationStateDefinition.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				diplomaticTermDiplomaticRelationStateDefinition = diplomaticTermDiplomaticRelationStateDefinition3;
			}
			if (diplomaticTermDiplomaticRelationStateDefinition3.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				diplomaticTermDiplomaticRelationStateDefinition2 = diplomaticTermDiplomaticRelationStateDefinition3;
			}
		}
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition4 = diplomaticTermDiplomaticRelationStateDefinition;
		Agent forceStatusAgentFromDiplomaticRelationState = this.GetForceStatusAgentFromDiplomaticRelationState(opponentEmpire, wantedDiplomaticRelationState);
		if (diplomaticTermDiplomaticRelationStateDefinition2 != null && forceStatusAgentFromDiplomaticRelationState != null && forceStatusAgentFromDiplomaticRelationState.Enable && forceStatusAgentFromDiplomaticRelationState.CriticityMax.Intensity >= this.diplomaticRelationStateAgentCriticityThreshold)
		{
			int num = -1;
			Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index) || (contract.EmpireWhichProposes.Index == opponentEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
			Func<DiplomaticTerm, bool> <>9__1;
			foreach (DiplomaticContract diplomaticContract2 in this.diplomacyContractRepositoryService.FindAll(match))
			{
				if (diplomaticContract2.State != DiplomaticContractState.Negotiation && diplomaticContract2.State != DiplomaticContractState.Signed)
				{
					IEnumerable<DiplomaticTerm> terms = diplomaticContract2.Terms;
					Func<DiplomaticTerm, bool> predicate;
					if ((predicate = <>9__1) == null)
					{
						predicate = (<>9__1 = ((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState && (term.Definition as DiplomaticTermDiplomaticRelationStateDefinition).DiplomaticRelationStateReference == wantedDiplomaticRelationState));
					}
					if (terms.Any(predicate))
					{
						num = Mathf.Max(num, diplomaticContract2.TurnAtTheBeginningOfTheState);
					}
				}
			}
			if (num >= 0 && this.game.Turn - num <= this.maximumNumberOfTurnsBetweenStatusAndForceStatus)
			{
				diplomaticTermDiplomaticRelationStateDefinition4 = diplomaticTermDiplomaticRelationStateDefinition2;
			}
		}
		if (diplomaticTermDiplomaticRelationStateDefinition4 == null)
		{
			if (name == DiplomaticRelationState.Names.ColdWar && wantedDiplomaticRelationState == DiplomaticRelationState.Names.Alliance)
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.Peace);
			}
			return;
		}
		DiplomaticTerm diplomaticTerm = new DiplomaticTermDiplomaticRelationState(diplomaticTermDiplomaticRelationStateDefinition4, this.empire, this.empire, opponentEmpire);
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
		this.WantedPrestigePoint = Mathf.Max(empirePointCost, this.WantedPrestigePoint);
		float num2;
		float num3;
		if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num2, out num3))
		{
			AILayer.LogError("Can't retrieve empire point account infos");
		}
		float num4;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num4, false);
		if (empirePointCost <= num2 || (this.AlwaysProcess && wantedDiplomaticRelationState == DiplomaticRelationState.Names.War && empirePointCost * 1.5f < num4))
		{
			AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
			contractRequest.Terms.Add(diplomaticTerm);
			if (this.AlwaysProcess)
			{
				if (wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
				{
					AILayer_Diplomacy.VictoryTargets[this.empire.Index] = opponentEmpire.Index;
				}
				contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
			}
			this.ContractRequests.Add(contractRequest);
			return;
		}
		if (name == DiplomaticRelationState.Names.Peace && wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
		{
			this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
			return;
		}
		if (name == DiplomaticRelationState.Names.Alliance && wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
		{
			this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
			return;
		}
		if (name == DiplomaticRelationState.Names.Alliance && wantedDiplomaticRelationState == DiplomaticRelationState.Names.ColdWar)
		{
			this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.Peace);
			return;
		}
		this.GenerateDiscussionContractRequest(opponentEmpire, diplomaticTermDiplomaticRelationStateDefinition4.Alignment);
	}

	private float GetGlobalScore(MajorEmpire empire)
	{
		float result = 0f;
		GameScore gameScore;
		if (empire.GameScores.TryGetValue(GameScores.Names.GlobalScore, out gameScore))
		{
			Diagnostics.Assert(gameScore != null);
			result = gameScore.Value;
		}
		return result;
	}

	public float GetAllyScore(global::Empire opponentEmpire)
	{
		MajorEmpire majorEmpire = (MajorEmpire)opponentEmpire;
		if (majorEmpire == null)
		{
			return 0f;
		}
		return this.GetAgentCriticityFor(majorEmpire, AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar);
	}

	public float GetWantWarScore(global::Empire opponentEmpire)
	{
		MajorEmpire majorEmpire = (MajorEmpire)opponentEmpire;
		if (majorEmpire == null)
		{
			return 0f;
		}
		return this.GetAgentCriticityFor(majorEmpire, AILayer_DiplomacyAmas.AgentNames.WarTermAgent);
	}

	public float GetWantTruceScore(global::Empire opponentEmpire)
	{
		MajorEmpire majorEmpire = (MajorEmpire)opponentEmpire;
		if (majorEmpire == null)
		{
			return 0f;
		}
		return this.GetAgentCriticityFor(majorEmpire, AILayer_DiplomacyAmas.AgentNames.TruceTermAgent);
	}

	public float GetGlobalWarNeed()
	{
		return this.aiLayerDiplomacyAmas.DiplomacyEvaluationAmas.AMAS.GetAgentCriticityMaxIntensity(AILayer_DiplomacyAmas.AgentNames.GlobalWarTermAgent);
	}

	private Agent GetForceStatusAgentFromDiplomaticRelationState(MajorEmpire opponentEmpire, StaticString diplomaticRelationState)
	{
		if (diplomaticRelationState == DiplomaticRelationState.Names.Truce)
		{
			AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
			return agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.ForceTruceTermAgent);
		}
		if (diplomaticRelationState == DiplomaticRelationState.Names.Peace)
		{
			AgentGroup agentGroupForEmpire2 = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
			return agentGroupForEmpire2.GetAgent(AILayer_DiplomacyAmas.AgentNames.ForcePeaceTermAgent);
		}
		if (diplomaticRelationState == DiplomaticRelationState.Names.Alliance)
		{
			AgentGroup agentGroupForEmpire3 = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
			return agentGroupForEmpire3.GetAgent(AILayer_DiplomacyAmas.AgentNames.ForceAllianceTermAgent);
		}
		return null;
	}

	private void OrderChangeDiplomaticContractState(DiplomaticContract diplomaticContract, DiplomaticContractState newState)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticContract");
		}
		if (diplomaticContract.State == newState)
		{
			AILayer.LogWarning("The diplomatic contract is already at state {0}", new object[]
			{
				newState
			});
			return;
		}
		if (diplomaticContract.Terms.Count == 0)
		{
			return;
		}
		OrderChangeDiplomaticContractState order = new OrderChangeDiplomaticContractState(diplomaticContract, newState);
		Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order);
	}

	private int DistanceToCurrentDiplomaticRelationState(MajorEmpire opponentEmpire, StaticString wantedDiplomaticRelationState)
	{
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
		StaticString name = diplomaticRelation.State.Name;
		int diplomaticRelationStateValue = DiplomaticRelationState.GetDiplomaticRelationStateValue(name);
		if (diplomaticRelationStateValue < 0)
		{
			return 0;
		}
		int diplomaticRelationStateValue2 = DiplomaticRelationState.GetDiplomaticRelationStateValue(wantedDiplomaticRelationState);
		if (diplomaticRelationStateValue2 < 0)
		{
			return 0;
		}
		return Mathf.Abs(diplomaticRelationStateValue2 - diplomaticRelationStateValue);
	}

	public void Tick()
	{
		if (this.coroutineActive || (this.currentContractRequest != null && this.currentContractRequest.State != AILayer_Diplomacy.ContractRequest.ContractRequestState.Done && this.currentContractRequest.State != AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed))
		{
			return;
		}
		this.currentContractRequest = null;
		for (int i = 0; i < this.ContractRequests.Count; i++)
		{
			AILayer_Diplomacy.ContractRequest contractRequest = this.ContractRequests[i];
			if (contractRequest.State == AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed)
			{
				this.ProcessContractRequest(contractRequest);
				this.currentContractRequest = contractRequest;
				break;
			}
		}
		if (this.currentContractRequest == null)
		{
			AILayer_Diplomacy.VictoryTargets[this.empire.Index] = -1;
			this.State = TickableState.NoTick;
		}
	}

	private void GetOrCreateContract(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		if (contractRequest == null)
		{
			throw new ArgumentNullException("contractRequest");
		}
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.RetrieveContract;
		DiplomaticContract diplomaticContract;
		if (!this.diplomacyService.TryGetActiveDiplomaticContract(this.empire, contractRequest.OpponentEmpire, out diplomaticContract))
		{
			OrderCreateDiplomaticContract orderCreateDiplomaticContract = new OrderCreateDiplomaticContract(this.empire, contractRequest.OpponentEmpire);
			Diagnostics.Assert(base.AIEntity.Empire.PlayerControllers.AI != null);
			Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderCreateDiplomaticContract, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
			contractRequest.CurrentOrderTicketNumber = orderCreateDiplomaticContract.TicketNumber;
		}
		else
		{
			this.PreFillContract(contractRequest);
		}
	}

	private void OrderFillContract(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		global::Empire empire = (contractRequest.Contract.EmpireWhichProposes.Index != this.empire.Index) ? contractRequest.Contract.EmpireWhichProposes : contractRequest.Contract.EmpireWhichReceives;
		empire.Refresh(true);
		this.empire.Refresh(true);
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Diagnostics.Log("ELCP: Empire {0} OrderFillContract base Thread {1}", new object[]
			{
				base.AIEntity.Empire.Index,
				Thread.CurrentThread.ManagedThreadId
			});
			ThreadPool.QueueUserWorkItem(delegate(object dataOrSomeDetails)
			{
				this.OrderFillContractELCP(contractRequest, true);
			});
			return;
		}
		Diagnostics.Log("ELCP: Empire {0} with {1}  OrderFillContract Starting Coroutine", new object[]
		{
			base.AIEntity.Empire.Index,
			empire
		});
		this.currentContractCoroutine = Amplitude.Coroutine.StartCoroutine(this.OrderFillContractCoroutine(contractRequest), null);
		this.currentContractCoroutine.Run();
		if (this.currentContractCoroutine.IsFinished)
		{
			this.currentContractCoroutine = null;
			this.coroutineActive = false;
			return;
		}
		this.coroutineActive = true;
		this.tickableRepositoryAIHelper.RegisterUpdate(this);
	}

	private void OrderProcessedEventHandler(object sender, TicketRaisedEventArgs ticketRaisedEventArgs)
	{
		AILayer_Diplomacy.ContractRequest contractRequest = this.ContractRequests.Find((AILayer_Diplomacy.ContractRequest match) => match.CurrentOrderTicketNumber == ticketRaisedEventArgs.Order.TicketNumber);
		if (contractRequest == null)
		{
			AILayer.LogError("Can't retrieve the contract request corresponding to order {0}", new object[]
			{
				ticketRaisedEventArgs.Order
			});
			return;
		}
		if (ticketRaisedEventArgs.Result != PostOrderResponse.Processed)
		{
			Diagnostics.LogWarning("ELCP: {1} The order {0} failed.", new object[]
			{
				ticketRaisedEventArgs.Order,
				this.empire
			});
			return;
		}
		switch (contractRequest.State)
		{
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.RetrieveContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderCreateDiplomaticContract);
			this.PreFillContract(contractRequest);
			return;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.PreFillContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractTermsCollection);
			this.OrderFillContract(contractRequest);
			return;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.FillContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractTermsCollection);
			this.ProposeContract(contractRequest);
			return;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.ProposeContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractState);
			contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Done;
			return;
		default:
			AILayer.LogError("Contract request state invalid ({1}) when receiving order {0}", new object[]
			{
				ticketRaisedEventArgs.Order,
				contractRequest.State
			});
			return;
		}
	}

	private void PreFillContract(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		if (contractRequest == null)
		{
			throw new ArgumentNullException("contractRequest");
		}
		DiplomaticContract contract;
		if (!this.diplomacyService.TryGetActiveDiplomaticContract(this.empire, contractRequest.OpponentEmpire, out contract))
		{
			AILayer.LogError("Failed to create a valid active contract.");
			contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed;
			return;
		}
		contractRequest.Contract = contract;
		Diagnostics.Assert(contractRequest.Contract != null);
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.PreFillContract;
		int count = contractRequest.Contract.Terms.Count;
		DiplomaticTermChange[] array = new DiplomaticTermChange[count + contractRequest.Terms.Count];
		for (int i = count - 1; i >= 0; i--)
		{
			DiplomaticTerm diplomaticTerm = contractRequest.Contract.Terms[i];
			array[i] = DiplomaticTermChange.Remove(diplomaticTerm.Index);
		}
		for (int j = 0; j < contractRequest.Terms.Count; j++)
		{
			DiplomaticTerm term = contractRequest.Terms[j];
			array[count + j] = DiplomaticTermChange.Add(term);
		}
		OrderChangeDiplomaticContractTermsCollection orderChangeDiplomaticContractTermsCollection = new OrderChangeDiplomaticContractTermsCollection(contractRequest.Contract, array);
		Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractTermsCollection, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
		contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractTermsCollection.TicketNumber;
	}

	private void ProcessContractRequest(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		if (contractRequest == null)
		{
			throw new ArgumentNullException("contractRequest");
		}
		Diagnostics.Assert(contractRequest.State == AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed);
		this.GetOrCreateContract(contractRequest);
	}

	private void ProposeContract(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		if (contractRequest == null)
		{
			throw new ArgumentNullException("contractRequest");
		}
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.ProposeContract;
		if (contractRequest.Contract.Terms.Count == 0)
		{
			contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed;
			return;
		}
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(contractRequest.Contract, this.empire);
		this.aiLayerAccountManager.TryGetAccount(AILayer_AccountManager.DiplomacyAccountName);
		float num;
		this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
		if (this.aiLayerAccountManager.TryMakeUnexpectedImmediateExpense(AILayer_AccountManager.DiplomacyAccountName, empirePointCost, 0f) || empirePointCost * 1.1f < num)
		{
			OrderChangeDiplomaticContractState orderChangeDiplomaticContractState = new OrderChangeDiplomaticContractState(contractRequest.Contract, DiplomaticContractState.Proposed);
			Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractState, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
			contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractState.TicketNumber;
			return;
		}
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed;
	}

	private void SynchronousMethod_ProcessContractRequests()
	{
		for (int i = 0; i < this.ContractRequests.Count; i++)
		{
			AILayer_Diplomacy.ContractRequest contractRequest = this.ContractRequests[i];
			if (contractRequest.State == AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed)
			{
				this.ProcessContractRequest(contractRequest);
				break;
			}
		}
	}

	private static float GetEmpireWealth(global::Empire empire)
	{
		Diagnostics.Assert(empire != null);
		DepartmentOfTheTreasury agency = empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(agency != null);
		float num = 0f;
		num += empire.GetPropertyValue(SimulationProperties.BankAccount);
		float propertyValue = empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		num += empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) * propertyValue;
		IDatabase<TradableCategoryDefinition> database = Databases.GetDatabase<TradableCategoryDefinition>(false);
		Diagnostics.Assert(database != null);
		IDatabase<ResourceDefinition> database2 = Databases.GetDatabase<ResourceDefinition>(false);
		foreach (ResourceDefinition resourceDefinition in database2)
		{
			if (resourceDefinition.ResourceType == ResourceDefinition.Type.Luxury || resourceDefinition.ResourceType == ResourceDefinition.Type.Strategic)
			{
				StaticString name = resourceDefinition.Name;
				float num2;
				if (agency.TryGetResourceStockValue(empire, name, out num2, true))
				{
					float num3;
					if (agency.TryGetNetResourceValue(empire, name, out num3, true))
					{
						float num4 = num2 + num3 * propertyValue;
						if (num4 >= 1f)
						{
							float num5 = TradableResource.GetPriceWithSalesTaxes(name, TradableTransactionType.Buyout, empire, 1f);
							num5 = Mathf.Pow(num5, 0.8f);
							num += num4 * num5;
						}
					}
				}
			}
		}
		return num / 8f;
	}

	[UtilityFunction("DiplomacyUtilityReceiver")]
	[UtilityFunction("DiplomacyUtilityProvider")]
	private static float UtilityFunction_Policy(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		return aiParameterValue;
	}

	[UtilityFunction("DiplomacyEconomyProvider")]
	private static float UtilityFunction_EconomyProvider(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		global::Empire empireWhichProvides = diplomaticTerm.EmpireWhichProvides;
		global::Empire empireWhichReceives = diplomaticTerm.EmpireWhichReceives;
		float? num = context.Get("MyEmpireIndex") as float?;
		float num2 = (float)empireWhichProvides.Index;
		float? num3 = num;
		global::Empire empire = (!(num2 == num3.GetValueOrDefault() & num3 != null)) ? empireWhichReceives : empireWhichProvides;
		float num4 = (float)empireWhichProvides.Index;
		num3 = num;
		global::Empire empire2 = (!(num4 == num3.GetValueOrDefault() & num3 != null)) ? empireWhichProvides : empireWhichReceives;
		float empireWealth = AILayer_Diplomacy.GetEmpireWealth(empire);
		float empireWealth2 = AILayer_Diplomacy.GetEmpireWealth(empire2);
		float num5 = 1f / Mathf.Max(1f, Mathf.Min(empireWealth2, empireWealth));
		float num6 = aiParameterValue * num5;
		if (diplomaticTerm is DiplomaticTermResourceExchange && num6 < 0f)
		{
			float num7 = -0.1f;
			if ((diplomaticTerm as DiplomaticTermResourceExchange).ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
			{
				num7 = -0.001f;
			}
			num6 = Mathf.Min(num6, (diplomaticTerm as DiplomaticTermResourceExchange).Amount * num7);
		}
		if (diplomaticTerm is DiplomaticTermBoosterExchange && num6 < 0f)
		{
			DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm as DiplomaticTermBoosterExchange;
			AILayer.Log("ELCP: {0} and {1} DiplomacyEconomyProvider {7}, wealth: {2} & {3}, num2 {4} num3 {5}, rawvalue {6}", new object[]
			{
				empireWhichProvides,
				empireWhichReceives,
				empireWealth,
				empireWealth2,
				num5,
				num6,
				aiParameterValue,
				diplomaticTermBoosterExchange.BoosterDefinitionName
			});
			num6 = aiParameterValue;
			AILayer.Log("ELCP: {0} and {1} DiplomacyEconomyProvider, returning: {2} ", new object[]
			{
				empireWhichProvides,
				empireWhichReceives,
				num6
			});
		}
		return num6;
	}

	[UtilityFunction("DiplomacyEconomyReceiver")]
	private static float UtilityFunction_EconomyReceiver(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		global::Empire empireWhichProvides = diplomaticTerm.EmpireWhichProvides;
		global::Empire empireWhichReceives = diplomaticTerm.EmpireWhichReceives;
		float? num = context.Get("MyEmpireIndex") as float?;
		float num2 = (float)empireWhichProvides.Index;
		float? num3 = num;
		global::Empire empire = (!(num2 == num3.GetValueOrDefault() & num3 != null)) ? empireWhichReceives : empireWhichProvides;
		float num4 = (float)empireWhichProvides.Index;
		num3 = num;
		global::Empire empire2 = (!(num4 == num3.GetValueOrDefault() & num3 != null)) ? empireWhichProvides : empireWhichReceives;
		float empireWealth = AILayer_Diplomacy.GetEmpireWealth(empire);
		float empireWealth2 = AILayer_Diplomacy.GetEmpireWealth(empire2);
		float num5 = 1f / Mathf.Max(1f, Mathf.Max(empireWealth2, empireWealth));
		float num6 = aiParameterValue * num5;
		if (diplomaticTerm is DiplomaticTermResourceExchange && num6 > 0f && (diplomaticTerm as DiplomaticTermResourceExchange).ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
		{
			float num7;
			if (!empireWhichReceives.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(empireWhichReceives.SimulationObject, DepartmentOfTheTreasury.Resources.EmpireMoney, out num7, false))
			{
				num7 = 0f;
			}
			float propertyValue = empireWhichReceives.GetPropertyValue(SimulationProperties.NetEmpireMoney);
			float num8 = Mathf.Abs(num7 / propertyValue);
			if (propertyValue < 0f && num8 < 10f)
			{
				float num9 = num5;
				if (num8 < 10f)
				{
					num9 = Mathf.Max(0.0001f, num9);
				}
				if (num8 < 5f)
				{
					num9 = Mathf.Max(0.001f, num9);
				}
				num6 = aiParameterValue * num9;
			}
		}
		if (diplomaticTerm is DiplomaticTermBoosterExchange && num6 > 0f)
		{
			num6 = aiParameterValue;
		}
		return num6;
	}

	[UtilityFunction("DiplomacyTechnologyProvider")]
	private static float UtilityFunction_TechnologyProvider(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		SimulationObject simulationObject2 = context.Get("OtherEmpire") as SimulationObject;
		float propertyValue = simulationObject.Children.Find((SimulationObject match) => match.Tags.Contains(AILayer_Diplomacy.ClassResearch)).GetPropertyValue(AILayer_Diplomacy.UnlockedTechnologyCount);
		float propertyValue2 = simulationObject2.Children.Find((SimulationObject match) => match.Tags.Contains(AILayer_Diplomacy.ClassResearch)).GetPropertyValue(AILayer_Diplomacy.UnlockedTechnologyCount);
		float b = propertyValue / Mathf.Max(1f, propertyValue2);
		float num = 1f / Mathf.Max(1f, b);
		return aiParameterValue * num;
	}

	[UtilityFunction("DiplomacyTechnologyReceiver")]
	private static float UtilityFunction_TechnologyReceiver(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		DepartmentOfScience agency = diplomaticTerm.EmpireWhichReceives.GetAgency<DepartmentOfScience>();
		DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = diplomaticTerm as DiplomaticTermTechnologyExchange;
		if (agency.CurrentTechnologyEraNumber == 6 && diplomaticTerm.EmpireWhichProposes != diplomaticTerm.EmpireWhichReceives && diplomaticTermTechnologyExchange != null && diplomaticTermTechnologyExchange.TechnologyDefinition != null)
		{
			bool flag = false;
			foreach (string technologyName in new List<string>
			{
				"TechnologyDefinitionDust6",
				"TechnologyDefinitionFood6",
				"TechnologyDefinitionIndustry5",
				"TechnologyDefinitionScience6",
				"TechnologyDefinitionEmpire1",
				"TechnologyDefinitionUnitImproved1"
			})
			{
				DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(technologyName);
				if (technologyState == DepartmentOfScience.ConstructibleElement.State.Queued || technologyState == DepartmentOfScience.ConstructibleElement.State.InProgress || technologyState == DepartmentOfScience.ConstructibleElement.State.Researched)
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				bool flag2 = diplomaticTerm.EmpireWhichReceives.GetPropertyValue(SimulationProperties.WarCount) >= 1f;
				string text = diplomaticTermTechnologyExchange.TechnologyDefinition.Name.ToString();
				if (DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition) != 6 && !text.Contains("Science") && !text.Contains("TradeRoute") && text != "TechnologyDefinitionDocks" && !text.Contains("AllBooster") && (!flag2 || !text.Contains("TechnologyDefinitionArmySize")))
				{
					return -8f;
				}
			}
		}
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		SimulationObject simulationObject2 = context.Get("OtherEmpire") as SimulationObject;
		float propertyValue = simulationObject.Children.Find((SimulationObject match) => match.Tags.Contains(AILayer_Diplomacy.ClassResearch)).GetPropertyValue(AILayer_Diplomacy.UnlockedTechnologyCount);
		float propertyValue2 = simulationObject2.Children.Find((SimulationObject match) => match.Tags.Contains(AILayer_Diplomacy.ClassResearch)).GetPropertyValue(AILayer_Diplomacy.UnlockedTechnologyCount);
		float b = propertyValue / Mathf.Max(1f, propertyValue2);
		float num = Mathf.Min(1f, b);
		return aiParameterValue * num;
	}

	[UtilityFunction("DiplomacyMilitarySupportProvider")]
	private static float UtilityFunction_MilitarySupportProvider(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		float num = 1f;
		DepartmentOfForeignAffairs agency = diplomaticTerm.EmpireWhichProvides.GetAgency<DepartmentOfForeignAffairs>();
		string x = agency.GetDiplomaticRelation(diplomaticTerm.EmpireWhichReceives.Index).State.Name;
		if (x == DiplomaticRelationState.Names.War)
		{
			num = 1.5f;
		}
		else if (x == DiplomaticRelationState.Names.Truce)
		{
			num = 1.25f;
		}
		else if (x == DiplomaticRelationState.Names.ColdWar)
		{
			num = 1f;
		}
		else if (x == DiplomaticRelationState.Names.Peace)
		{
			num = 0.75f;
		}
		else if (x == DiplomaticRelationState.Names.Alliance)
		{
			num = 0.5f;
		}
		float num2 = aiParameterValue * num;
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		float propertyValue = simulationObject.GetPropertyValue(SimulationProperties.WarCount);
		float propertyValue2 = simulationObject.GetPropertyValue(SimulationProperties.TruceCount);
		float num3 = 1f - (propertyValue + 0.5f * propertyValue2) / 8f;
		num3 *= num3;
		return num2 / num3;
	}

	[UtilityFunction("DiplomacyMilitarySupportReceiver")]
	private static float UtilityFunction_MilitarySupportReceiver(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		float num = 1f;
		DepartmentOfForeignAffairs agency = diplomaticTerm.EmpireWhichProvides.GetAgency<DepartmentOfForeignAffairs>();
		string x = agency.GetDiplomaticRelation(diplomaticTerm.EmpireWhichReceives.Index).State.Name;
		if (x == DiplomaticRelationState.Names.War)
		{
			num = 0.5f;
		}
		else if (x == DiplomaticRelationState.Names.Truce)
		{
			num = 0.75f;
		}
		else if (x == DiplomaticRelationState.Names.ColdWar)
		{
			num = 1f;
		}
		else if (x == DiplomaticRelationState.Names.Peace)
		{
			num = 1.25f;
		}
		else if (x == DiplomaticRelationState.Names.Alliance)
		{
			num = 1.5f;
		}
		float num2 = aiParameterValue * num;
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		float propertyValue = simulationObject.GetPropertyValue(SimulationProperties.WarCount);
		float propertyValue2 = simulationObject.GetPropertyValue(SimulationProperties.TruceCount);
		float num3 = 1f - (propertyValue + 0.5f * propertyValue2) / 8f;
		num3 *= num3;
		return num2 / num3;
	}

	[UtilityFunction("DiplomacyOrbProvider")]
	private static float UtilityFunction_OrbProvider(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		SimulationObject simulationObject2 = context.Get("OtherEmpire") as SimulationObject;
		float propertyValue = simulationObject.GetPropertyValue(SimulationProperties.OrbStock);
		float propertyValue2 = simulationObject2.GetPropertyValue(SimulationProperties.OrbStock);
		float num = Mathf.Max(0.01f, 1f / Mathf.Max(1f, Mathf.Min(propertyValue, propertyValue2)));
		if (diplomaticTerm.EmpireWhichProvides.SimulationObject.Tags.Contains(AILayer_Diplomacy.FactionTraitTechnologyDefinitionOrbUnlock18WinterShifters))
		{
			num *= 1.5f;
		}
		return aiParameterValue * num;
	}

	[UtilityFunction("DiplomacyOrbReceiver")]
	private static float UtilityFunction_OrbReceiver(DiplomaticTerm diplomaticTerm, InterpreterContext context, float aiParameterValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		SimulationObject simulationObject2 = context.Get("OtherEmpire") as SimulationObject;
		float num = Mathf.Max(1f, simulationObject.GetPropertyValue(SimulationProperties.OrbStock));
		float num2 = Mathf.Max(1f, simulationObject2.GetPropertyValue(SimulationProperties.OrbStock));
		float num3 = 1f / (num2 + num);
		if (diplomaticTerm.EmpireWhichReceives.SimulationObject.Tags.Contains(AILayer_Diplomacy.FactionTraitTechnologyDefinitionOrbUnlock18WinterShifters))
		{
			num3 *= 1.5f;
		}
		return aiParameterValue * num3;
	}

	public float WantedPrestigePoint { get; private set; }

	public DecisionResult EvaluateDecision(DiplomaticTerm term, global::Empire otherEmpire, EvaluationData<DiplomaticTerm, InterpreterContext> evaluationData = null)
	{
		Diagnostics.Assert(this.diplomaticTermEvaluator != null && this.diplomaticTermEvaluator.Context != null);
		this.diplomaticTermEvaluator.Context.Clear();
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Enter(this.diplomaticTermEvaluator.Context);
		}
		DecisionResult result = this.diplomaticTermEvaluator.Evaluate(term, null);
		this.lastEmpireInInterpreter = null;
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Exit(this.diplomaticTermEvaluator.Context);
		}
		return result;
	}

	public void EvaluateDecisions(IEnumerable<DiplomaticTerm> terms, global::Empire otherEmpire, ref List<DecisionResult> decisions)
	{
		Diagnostics.Assert(this.diplomaticTermEvaluator != null && this.diplomaticTermEvaluator.Context != null);
		this.diplomaticTermEvaluator.Context.Clear();
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Enter(this.diplomaticTermEvaluator.Context);
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			EvaluationData<DiplomaticTerm, InterpreterContext> evaluationData = new EvaluationData<DiplomaticTerm, InterpreterContext>();
			this.diplomaticTermEvaluator.Evaluate(terms, ref decisions, evaluationData);
			evaluationData.Turn = this.game.Turn;
			this.DebugEvaluationsHistoric.Add(evaluationData);
		}
		else
		{
			this.diplomaticTermEvaluator.Evaluate(terms, ref decisions, null);
		}
		this.lastEmpireInInterpreter = null;
		if (ELCPUtilities.UseELCPMultiThreading)
		{
			Monitor.Exit(this.diplomaticTermEvaluator.Context);
		}
	}

	private void BeginElementEvaluationDelegate(DiplomaticTerm diplomaticTerm, InterpreterContext interpreterContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		global::Empire empireWhichProvides = diplomaticTerm.EmpireWhichProvides;
		global::Empire empireWhichReceives = diplomaticTerm.EmpireWhichReceives;
		Diagnostics.Assert(this.empire != null && empireWhichProvides != null && empireWhichReceives != null);
		global::Empire empire = (empireWhichProvides.Index != this.empire.Index) ? empireWhichProvides : empireWhichReceives;
		DiplomaticContract diplomaticContract = null;
		if (this.CurrentAnswerContract != null)
		{
			diplomaticContract = this.CurrentAnswerContract;
		}
		else
		{
			Predicate<DiplomaticContract> match2 = (DiplomaticContract contract) => contract.State == DiplomaticContractState.Negotiation && contract.EmpireWhichProposes == diplomaticTerm.EmpireWhichProposes && contract.EmpireWhichReceives == ((diplomaticTerm.EmpireWhichProposes == this.empire) ? empire : this.empire);
			List<DiplomaticContract> list = this.diplomacyContractRepositoryService.FindAll(match2).ToList<DiplomaticContract>();
			if (list != null && list.Count > 0)
			{
				diplomaticContract = list[0];
			}
		}
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		interpreterContext.Register("Provider", empireWhichProvides.SimulationObject);
		interpreterContext.Register("Receiver", empireWhichReceives.SimulationObject);
		interpreterContext.Register("MyEmpire", this.empire.SimulationObject);
		interpreterContext.Register("OtherEmpire", empire.SimulationObject);
		interpreterContext.Register("MyEmpireIndex", this.empire.Index);
		interpreterContext.Register("NeedsVictoryReaction", (!this.NeedsVictoryReaction[empire.Index]) ? 0f : 1f);
		float num = 1000f;
		float num2 = 0f;
		if (this.empire.IsControlledByAI && !empire.IsControlledByAI)
		{
			if (this.MilitaryPowerDif > 0f)
			{
				num = 0.1f;
			}
			else if (this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > empire.GetPropertyValue(SimulationProperties.LandMilitaryPower))
			{
				num = 1f;
			}
			if (diplomaticTerm.Definition.Name == DiplomaticTermDefinition.Names.WarToTruce && this.MilitaryPowerDif > 0.5f && this.empire.GetPropertyValue(SimulationProperties.WarCount) == 1f)
			{
				using (IEnumerator<City> enumerator = empire.GetAgency<DepartmentOfTheInterior>().Cities.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						if (enumerator.Current.Ownership[this.empire.Index] > 0f)
						{
							num2 = 20f + (this.MilitaryPowerDif - 0.5f) * 40f;
							break;
						}
					}
				}
			}
		}
		interpreterContext.Register("PowerDifMalus", num2);
		interpreterContext.Register("UpperCap", num);
		interpreterContext.SimulationObject = this.empire.SimulationObject;
		this.FillInterpreterContext(empire, interpreterContext);
		DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = diplomaticTerm as DiplomaticTermTechnologyExchange;
		if (diplomaticTermTechnologyExchange != null)
		{
			DecisionResult decisionResult = this.aiLayerResearch.EvaluateTechnology(diplomaticTermTechnologyExchange.TechnologyDefinition);
			float num3 = (float)this.departmentOfScience.GetTechnologyRemainingTurn(diplomaticTermTechnologyExchange.TechnologyDefinition);
			int technologyEraNumber = DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition);
			interpreterContext.Register("TechnologyEra", (float)technologyEraNumber);
			interpreterContext.Register("TechnologyEvaluationScore", (decisionResult.FailureFlags != null && decisionResult.FailureFlags.Length != 0) ? 0f : decisionResult.Score);
			interpreterContext.Register("TechnologyRemainingTurn", num3);
			bool flag = decisionResult.Score == 0f && technologyEraNumber < 6;
			if (diplomaticTermTechnologyExchange.EmpireWhichProposes == this.empire && this.DiplomacyFocus)
			{
				flag = false;
			}
			interpreterContext.Register("DoesNotWantTech", flag ? 1f : 0f);
		}
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		float worldExplorationRatio = service.GetWorldExplorationRatio(this.empire);
		float worldExplorationRatio2 = service.GetWorldExplorationRatio(empire);
		float num4 = Mathf.Max(0.1f, worldExplorationRatio2) / Mathf.Max(0.1f, worldExplorationRatio);
		interpreterContext.Register("TheirExplorationlead", num4);
		float propertyValue = this.empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor);
		float num5 = empire.GetPropertyValue(SimulationProperties.EmpireScaleFactor) / propertyValue;
		interpreterContext.Register("TheirScaleLead", num5);
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm as DiplomaticTermResourceExchange;
		if (diplomaticTermResourceExchange != null)
		{
			interpreterContext.Register("ResourceAmount", diplomaticTermResourceExchange.Amount);
			float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(diplomaticTermResourceExchange.ResourceName, TradableTransactionType.Buyout, empireWhichReceives, 1f);
			interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes);
			float num6;
			empireWhichProvides.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(empireWhichProvides.SimulationObject, diplomaticTermResourceExchange.ResourceName, out num6, true);
			interpreterContext.Register("ProviderResourceStock", num6);
			float num7;
			empireWhichReceives.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(empireWhichReceives.SimulationObject, diplomaticTermResourceExchange.ResourceName, out num7, true);
			interpreterContext.Register("ReceiverResourceStock", num7);
			float num8 = 1f;
			if (!empire.IsControlledByAI && this.empire.IsControlledByAI && this.departmentOfForeignAffairs.IsInWarWithSomeone() && diplomaticTermResourceExchange.EmpireWhichProposes.Index != this.empire.Index)
			{
				bool flag2 = false;
				if (diplomaticContract != null)
				{
					using (IEnumerator<DiplomaticTerm> enumerator2 = diplomaticContract.Terms.GetEnumerator())
					{
						while (enumerator2.MoveNext())
						{
							if (enumerator2.Current.Definition.Name == DiplomaticTermDefinition.Names.WarToTruce)
							{
								flag2 = true;
								break;
							}
						}
					}
				}
				if (!flag2)
				{
					num8 = this.resourceWarReserveFactor;
				}
			}
			interpreterContext.Register("WarReserveFactor", num8);
		}
		DiplomaticTermCityExchange diplomaticTermCityExchange = diplomaticTerm as DiplomaticTermCityExchange;
		if (diplomaticTermCityExchange != null)
		{
			Diagnostics.Assert(this.gameEntityRepositoryService != null);
			IGameEntity gameEntity;
			if (this.gameEntityRepositoryService.TryGetValue(diplomaticTermCityExchange.CityGUID, out gameEntity))
			{
				City city = gameEntity as City;
				if (city != null)
				{
					interpreterContext.SimulationObject = city.SimulationObject;
					Diagnostics.Assert(this.aiLayerResourceAmas != null);
					AgentGroup cityAgentGroup = this.aiLayerResourceAmas.GetCityAgentGroup(city);
					if (cityAgentGroup != null)
					{
						Agent agent = cityAgentGroup.GetAgent(AILayer_ResourceAmas.AgentNames.IndustryReferenceTurnCount);
						Diagnostics.Assert(agent != null && agent.CriticityMax != null);
						interpreterContext.Register("IndustryReferenceTurnCountAgentCriticity", agent.CriticityMax.Intensity);
					}
					else
					{
						AILayer.LogWarning("Can't retrieve the city agent for city {0}.", new object[]
						{
							city.GUID
						});
					}
					float num9 = 0f;
					for (int i = 0; i < city.Region.Borders.Length; i++)
					{
						Region.Border border = city.Region.Borders[i];
						Region region = this.worldPositionningService.GetRegion(border.NeighbourRegionIndex);
						if (region.IsRegionColonized() && region.Owner != null && region.Owner.Index == this.empire.Index)
						{
							num9 += (float)border.WorldPositions.Length;
						}
					}
					interpreterContext.Register("BorderLengthCommonWithMyEmpire", num9);
					interpreterContext.Register("IsCapital", (!city.SimulationObject.Tags.Contains(City.TagMainCity)) ? 0f : 1f);
					float num10 = 0f;
					int num11 = 0;
					if (empireWhichProvides.Index == this.empire.Index && !empireWhichReceives.IsControlledByAI && (this.departmentOfTheInterior.Cities.Count < 4 || (diplomaticTermCityExchange.Definition.Name != DiplomaticTermCityExchange.MimicsCityDeal && num > 0f)))
					{
						num11 = 1;
					}
					if (diplomaticTermCityExchange.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
					{
						num10 = 1f;
						if (empireWhichProvides.Index == this.empire.Index)
						{
							if (empireWhichReceives.IsControlledByAI)
							{
								num10 /= 2f;
							}
							if (num11 == 0)
							{
								float num12 = 0f;
								for (int j = 0; j < this.departmentOfTheInterior.Cities.Count; j++)
								{
									num12 += this.departmentOfTheInterior.Cities[j].GetPropertyValue(SimulationProperties.Workers);
								}
								if (city.GetPropertyValue(SimulationProperties.Workers) > num12 / 10f)
								{
									num11++;
								}
							}
						}
					}
					else if (empireWhichReceives.Index == this.empire.Index && this.canInfect && !this.departmentOfPlanificationAndDevelopment.HasIntegratedFaction(empireWhichProvides.Faction))
					{
						num10 = 0.75f;
					}
					else if (empireWhichProvides.Index == this.empire.Index && !empireWhichReceives.IsControlledByAI && num11 == 0)
					{
						if (this.GetMilitaryPowerDif(false) >= 0f)
						{
							num11++;
						}
						else if (diplomaticContract != null)
						{
							int num13 = 0;
							int num14 = 0;
							bool flag3 = false;
							foreach (DiplomaticTerm diplomaticTerm2 in diplomaticContract.Terms)
							{
								if (diplomaticTerm2 is DiplomaticTermCityExchange)
								{
									if (diplomaticTerm2.EmpireWhichProvides.Index == this.empire.Index)
									{
										num13++;
										if ((diplomaticTerm2 as DiplomaticTermCityExchange).CityGUID == diplomaticTermCityExchange.CityGUID)
										{
											flag3 = true;
										}
									}
									else
									{
										num14++;
									}
								}
							}
							if ((!flag3 && num13 > num14) || (flag3 && num13 > num14 + 1))
							{
								num11++;
							}
						}
					}
					num11 += city.Districts.Count((District district) => district.Type == DistrictType.Extension && district.SimulationObject.Tags.Contains(DistrictImprovementDefinition.ReadOnlyWonderClass));
					interpreterContext.Register("MimicsDeal", num10);
					interpreterContext.Register("NeverAgree", (float)num11);
				}
				else
				{
					AILayer.LogError("The game entity {0} is not a city.", new object[]
					{
						diplomaticTermCityExchange.CityGUID
					});
				}
			}
			else
			{
				AILayer.LogError("Can't retrieve game entity {0}.", new object[]
				{
					diplomaticTermCityExchange.CityGUID
				});
			}
		}
		DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm as DiplomaticTermBoosterExchange;
		if (diplomaticTermBoosterExchange != null)
		{
			interpreterContext.SimulationObject = this.empire.SimulationObject;
			float num15 = (float)((diplomaticTermBoosterExchange.BoosterGUID == null) ? 0 : diplomaticTermBoosterExchange.BoosterGUID.Length);
			interpreterContext.Register("ResourceAmount", num15);
			Diagnostics.Assert(this.departmentOfPlanificationAndDevelopment != null);
			float num16 = (float)this.departmentOfPlanificationAndDevelopment.CountBoosters((BoosterDefinition match) => match.Name == diplomaticTermBoosterExchange.BoosterDefinitionName);
			interpreterContext.Register("BoosterCount", num16);
			float priceWithSalesTaxes2 = TradableBooster.GetPriceWithSalesTaxes(diplomaticTermBoosterExchange.BoosterDefinitionName, TradableTransactionType.Sellout, this.empire, 1f);
			interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes2);
			float num17 = 1f;
			if (this.departmentOfScience.GetTechnologyState("TechnologyDefinitionAllBoosterLevel1") == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				num17 += 1f;
			}
			if (this.departmentOfScience.GetTechnologyState("TechnologyDefinitionAllBoosterLevel2") == DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				num17 += 1f;
			}
			interpreterContext.Register("Boostertechs", num17 / 3f);
		}
		DiplomaticTermProposal diplomaticTermProposal = diplomaticTerm as DiplomaticTermProposal;
		if (diplomaticTermProposal != null)
		{
			interpreterContext.SimulationObject = this.empire;
			bool flag4 = diplomaticTerm.EmpireWhichReceives == this.empire;
			bool flag5 = this.departmentOfForeignAffairs.IsAtWarWith(diplomaticTermProposal.ChosenEmpire);
			bool flag6 = agency.IsAtWarWith(diplomaticTermProposal.ChosenEmpire);
			interpreterContext.Register("AskedByMe", (!flag4) ? 1 : 0);
			interpreterContext.Register("ProposalScore", 0f);
			interpreterContext.Register("ThirdParty", diplomaticTermProposal.ChosenEmpire.SimulationObject);
			interpreterContext.Register("AtWarWithThirdParty", flag5 ? 1 : 0);
			interpreterContext.Register("ThirdPartyNearVictory", (!this.NeedsVictoryReaction[diplomaticTermProposal.ChosenEmpireIndex]) ? 0f : 1f);
			if (!flag6 && diplomaticContract != null)
			{
				foreach (DiplomaticTerm diplomaticTerm3 in diplomaticContract.Terms)
				{
					DiplomaticTermProposal diplomaticTermProposal2 = diplomaticTerm3 as DiplomaticTermProposal;
					if (diplomaticTermProposal2 != null && diplomaticTermProposal2.EmpireWhichProvides != diplomaticTermProposal.EmpireWhichProvides && diplomaticTermProposal2.Definition.Name == DiplomaticTermDefinition.Names.AskToDeclareWar && diplomaticTermProposal2.ChosenEmpireIndex == diplomaticTermProposal.ChosenEmpireIndex && diplomaticTermProposal2.ChosenEmpireIndex >= 0)
					{
						flag6 = true;
						break;
					}
				}
			}
			interpreterContext.Register("ReceiverAtWarWithThirdParty", flag6 ? 1 : 0);
			this.FillInterpreterContext(diplomaticTermProposal.ChosenEmpire, interpreterContext);
		}
		DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange = diplomaticTerm as DiplomaticTermPrisonerExchange;
		if (diplomaticTermPrisonerExchange != null)
		{
			interpreterContext.SimulationObject = this.empire;
			bool flag7 = diplomaticTerm.EmpireWhichReceives == this.empire;
			interpreterContext.Register("AskedByMe", (!flag7) ? 1 : 0);
			IGameEntity gameEntity2;
			if (this.gameEntityRepositoryService.TryGetValue(diplomaticTermPrisonerExchange.HeroGuid, out gameEntity2) && gameEntity2 is Unit)
			{
				Unit unit = gameEntity2 as Unit;
				interpreterContext.Register("Hero", unit);
				float priceWithSalesTaxes3 = TradableUnit.GetPriceWithSalesTaxes(unit, TradableTransactionType.Buyout, empireWhichReceives, 1f);
				interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes3);
				float num18 = 30f * base.AIEntity.Empire.SimulationObject.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
				int numberOfTurnBeforeRelease = this.departmentOfEducation.GetNumberOfTurnBeforeRelease(diplomaticTermPrisonerExchange.HeroGuid);
				interpreterContext.Register("Turnsleft", (float)numberOfTurnBeforeRelease / num18);
			}
		}
		DiplomaticTermFortressExchange fortressExchange = diplomaticTerm as DiplomaticTermFortressExchange;
		if (fortressExchange != null)
		{
			global::Empire empire2 = this.game.Empires.First((global::Empire match) => match is NavalEmpire);
			if (empire2 != null)
			{
				PirateCouncil agency2 = empire2.GetAgency<PirateCouncil>();
				if (agency2 != null)
				{
					Fortress fortress = agency2.Fortresses.FirstOrDefault((Fortress match) => match.GUID == fortressExchange.FortressGUID);
					if (fortress != null)
					{
						int num19 = 0;
						if (fortress.Region.Owner == empireWhichProvides)
						{
							num19 = 1;
						}
						int num20 = 0;
						int num21 = 0;
						int num22 = 0;
						for (int k = 0; k < agency2.Fortresses.Count; k++)
						{
							if (agency2.Fortresses[k].Region == fortress.Region)
							{
								num20++;
								if (agency2.Fortresses[k].Occupant == empireWhichProvides)
								{
									num21++;
								}
								if (agency2.Fortresses[k].Occupant == empireWhichReceives)
								{
									num22++;
								}
							}
						}
						interpreterContext.SimulationObject = fortress.SimulationObject;
						interpreterContext.Register("IsRegionControlled", num19);
						interpreterContext.Register("TotalNumberOfFortressesInRegion", num20);
						interpreterContext.Register("ProviderNumberOfFortressesInRegion", num21);
						interpreterContext.Register("ReceiverNumberOfFortressesInRegion", num22);
						float num23 = 0f;
						NavyRegionData navyRegionData = this.aiLayerNavy.NavyRegions.Find((BaseNavyRegionData match) => match.WaterRegionIndex == fortress.Region.Index) as NavyRegionData;
						if (navyRegionData != null)
						{
							num23 = navyRegionData.RegionScore;
						}
						interpreterContext.Register("MyRegionScore", num23);
						int num24 = 0;
						int num25 = 0;
						bool flag8 = false;
						if (diplomaticContract != null)
						{
							foreach (DiplomaticTerm diplomaticTerm4 in diplomaticContract.Terms)
							{
								DiplomaticTermFortressExchange fortressExchange2 = diplomaticTerm4 as DiplomaticTermFortressExchange;
								if (fortressExchange2 != null)
								{
									Fortress fortress2 = agency2.Fortresses.FirstOrDefault((Fortress match3) => match3.GUID == fortressExchange2.FortressGUID);
									if (fortress2.Region == fortress.Region && fortress2.GUID != fortress.GUID)
									{
										num24++;
									}
									if (fortress2.GUID == fortress.GUID)
									{
										num25 = 1;
									}
									if (fortress2.Occupant == empireWhichReceives && fortress2.Region == fortress.Region)
									{
										flag8 = true;
										break;
									}
								}
							}
						}
						float num26 = 0f;
						if (num20 - num22 - num24 == 1 && num25 == 0 && !flag8)
						{
							num26 = 1f;
						}
						if (num20 - num22 - num24 - num25 == 0 && !flag8)
						{
							num26 = 1f / (float)(num24 + num25);
						}
						interpreterContext.Register("RegionCompletion", num26);
						bool flag9 = false;
						if (diplomaticContract != null)
						{
							foreach (DiplomaticTerm diplomaticTerm5 in diplomaticContract.Terms)
							{
								DiplomaticTermDiplomaticRelationState diplomaticTermDiplomaticRelationState = diplomaticTerm5 as DiplomaticTermDiplomaticRelationState;
								if (diplomaticTermDiplomaticRelationState != null)
								{
									DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = diplomaticTermDiplomaticRelationState.Definition as DiplomaticTermDiplomaticRelationStateDefinition;
									if (diplomaticTermDiplomaticRelationStateDefinition != null && diplomaticTermDiplomaticRelationStateDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.Truce)
									{
										flag9 = true;
										break;
									}
								}
							}
						}
						interpreterContext.Register("IsTruceContract", flag9 ? 1 : 0);
					}
				}
			}
		}
	}

	private void FillContract(DiplomaticContract diplomaticContract, ref List<DiplomaticTerm> diplomaticTerms)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		if (diplomaticTerms == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		global::Empire empire = (diplomaticContract.EmpireWhichProposes.Index != this.empire.Index) ? diplomaticContract.EmpireWhichProposes : diplomaticContract.EmpireWhichReceives;
		AILayer_Diplomacy layer = null;
		AILayer_ResourceManager othersResourceManager = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				layer = entity.GetLayer<AILayer_Diplomacy>();
				othersResourceManager = entity.GetLayer<AILayer_ResourceManager>();
			}
		}
		bool flag = false;
		bool flag2 = false;
		global::Empire thirdParty = null;
		foreach (DiplomaticTerm diplomaticTerm in diplomaticContract.Terms)
		{
			DiplomaticTermProposal diplomaticTermProposal = diplomaticTerm as DiplomaticTermProposal;
			if (diplomaticTermProposal != null && diplomaticTermProposal.Definition.Name == DiplomaticTermDefinition.Names.AskToDeclareWar)
			{
				thirdParty = diplomaticTermProposal.ChosenEmpire;
			}
		}
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.diplomaticTermsCacheList != null);
		this.diplomaticTermsCacheList.Clear();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && !(diplomaticTermDefinition is DiplomaticTermDiplomaticRelationStateDefinition))
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, empire, ref this.diplomaticTermsCacheList);
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, empire, this.empire, ref this.diplomaticTermsCacheList);
					}
				}
			}
		}
		Diagnostics.Assert(this.availableTermEvaluationsCacheList != null);
		this.availableTermEvaluationsCacheList.Clear();
		for (int j = 0; j < this.diplomaticTermsCacheList.Count; j++)
		{
			DiplomaticTerm diplomaticTerm2 = this.diplomaticTermsCacheList[j];
			DiplomaticTermProposal diplomaticTermProposal2 = diplomaticTerm2 as DiplomaticTermProposal;
			if (diplomaticTermProposal2 != null && ((diplomaticTermProposal2.EmpireWhichProvides == this.empire && this.empire.GetPropertyValue(SimulationProperties.WarCount) == 0f) || (diplomaticTermProposal2.EmpireWhichProvides == empire && empire.GetPropertyValue(SimulationProperties.WarCount) == 0f)))
			{
				this.DecideIfKeepProposal(diplomaticTermProposal2, diplomaticContract, agency, thirdParty, ref flag, ref flag2);
			}
			DiplomaticTermCityExchange diplomaticTermCityExchange = diplomaticTerm2 as DiplomaticTermCityExchange;
			if (diplomaticTermCityExchange != null && diplomaticTermCityExchange.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
			{
				this.diplomaticTermsCacheList.RemoveAt(j);
				j--;
			}
			else
			{
				DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm2 as DiplomaticTermBoosterExchange;
				if (diplomaticTermBoosterExchange != null && this.ModifyOrRemoveBoosterExchange(empire, othersResourceManager, ref diplomaticTermBoosterExchange))
				{
					this.diplomaticTermsCacheList.RemoveAt(j);
					j--;
				}
				else
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation = new AILayer_Diplomacy.TermEvaluation(diplomaticTerm2);
					DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm2 as DiplomaticTermResourceExchange;
					if (diplomaticTermResourceExchange != null)
					{
						float num;
						float maximumAmount;
						this.GetDiplomaticTermAmountLimits(diplomaticTerm2, out num, out maximumAmount);
						diplomaticTermResourceExchange.Amount = num;
						termEvaluation.MinimumAmount = num;
						termEvaluation.MaximumAmount = maximumAmount;
					}
					this.availableTermEvaluationsCacheList.Add(termEvaluation);
				}
			}
		}
		Diagnostics.Assert(this.diplomaticTermsCacheList.Count == this.availableTermEvaluationsCacheList.Count);
		EvaluationData<DiplomaticTerm, InterpreterContext> evaluationData = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			evaluationData = new EvaluationData<DiplomaticTerm, InterpreterContext>();
			evaluationData.Turn = this.game.Turn;
		}
		for (int k = 0; k < this.diplomaticTermsCacheList.Count; k++)
		{
			AILayer_Diplomacy.TermEvaluation termEvaluation2 = this.availableTermEvaluationsCacheList[k];
			termEvaluation2.SetAmount(termEvaluation2.MinimumAmount);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				termEvaluation2.MinimumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, evaluationData).Score;
			}
			else
			{
				termEvaluation2.MinimumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, null).Score;
			}
			Diagnostics.Assert(!float.IsNaN(termEvaluation2.MinimumAmountScore), "Evaluation of term '{0}' return a NaN value.", new object[]
			{
				termEvaluation2.Term
			});
			termEvaluation2.SetAmount(termEvaluation2.MaximumAmount);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				termEvaluation2.MaximumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, evaluationData).Score;
			}
			else
			{
				termEvaluation2.MaximumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, null).Score;
			}
			Diagnostics.Assert(!float.IsNaN(termEvaluation2.MaximumAmountScore), "Evaluation of term '{0}' return a NaN value.", new object[]
			{
				termEvaluation2.Term
			});
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			this.DebugEvaluationsHistoric.Add(evaluationData);
		}
		float num3 = this.AnalyseContractProposition(diplomaticContract);
		this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation t) => t.Term is DiplomaticTermTechnologyExchange && t.Term.EmpireWhichProvides == empire && t.MaximumAmountScore <= 0f);
		if (num3 > 0f && layer.IsActive())
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation t) => !(t.Term is DiplomaticTermResourceExchange) && layer.EvaluateDecision(t.Term, this.empire, null).Score <= 0f);
		}
		int num2 = 0;
		foreach (AILayer_Diplomacy.TermEvaluation termEvaluation3 in this.availableTermEvaluationsCacheList)
		{
			if (!(termEvaluation3.Term is DiplomaticTermFortressExchange) && !(termEvaluation3.Term is DiplomaticTermCityExchange) && !(termEvaluation3.Term is DiplomaticTermProposal) && termEvaluation3.Term.EmpireWhichProvides == this.empire && Mathf.Abs(termEvaluation3.MaximumAmountScore) > num3 / 6f)
			{
				num2++;
			}
		}
		if (!flag)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == this.empire && x.Term is DiplomaticTermProposal);
		}
		if (num2 > 5)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == this.empire && (x.Term is DiplomaticTermFortressExchange || x.Term is DiplomaticTermCityExchange));
		}
		num2 = 0;
		foreach (AILayer_Diplomacy.TermEvaluation termEvaluation4 in this.availableTermEvaluationsCacheList)
		{
			if (!(termEvaluation4.Term is DiplomaticTermFortressExchange) && !(termEvaluation4.Term is DiplomaticTermCityExchange) && !(termEvaluation4.Term is DiplomaticTermProposal) && termEvaluation4.Term.EmpireWhichProvides == empire && Mathf.Abs(termEvaluation4.MaximumAmountScore) > num3 / 6f)
			{
				num2++;
			}
		}
		if (!flag2)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == empire && x.Term is DiplomaticTermProposal);
		}
		if (num2 > 5)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == empire && (x.Term is DiplomaticTermFortressExchange || x.Term is DiplomaticTermCityExchange || x.Term is DiplomaticTermProposal));
		}
		if (layer != null && layer.IsActive())
		{
			this.availableTermEvaluationsCacheList.Sort(delegate(AILayer_Diplomacy.TermEvaluation left, AILayer_Diplomacy.TermEvaluation right)
			{
				float num17 = left.MaximumAmountScore;
				float num18 = right.MaximumAmountScore;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				bool flag6 = false;
				if (Mathf.Abs(num17) > Mathf.Abs(num3 / 6f))
				{
					if (num17 < 0f)
					{
						flag4 = true;
						num17 = -layer.EvaluateDecision(left.Term, this.empire, null).Score;
					}
					if (num17 > 0f)
					{
						flag3 = true;
						num17 += layer.EvaluateDecision(left.Term, this.empire, null).Score;
					}
				}
				if (Mathf.Abs(num18) > Mathf.Abs(num3 / 6f))
				{
					if (num18 < 0f)
					{
						flag6 = true;
						num18 = -layer.EvaluateDecision(right.Term, this.empire, null).Score;
					}
					if (num18 > 0f)
					{
						flag5 = true;
						num18 += layer.EvaluateDecision(right.Term, this.empire, null).Score;
					}
				}
				if (flag4 && !flag6)
				{
					return -1;
				}
				if (!flag4 && flag6)
				{
					return 1;
				}
				if (flag3 && !flag5)
				{
					return 1;
				}
				if (!flag3 && flag5)
				{
					return -1;
				}
				if ((flag4 && flag6) || (flag3 && flag5))
				{
					return num17.CompareTo(num18);
				}
				return left.MaximumAmountScore.CompareTo(right.MaximumAmountScore);
			});
		}
		else
		{
			this.availableTermEvaluationsCacheList.Sort((AILayer_Diplomacy.TermEvaluation left, AILayer_Diplomacy.TermEvaluation right) => left.MaximumAmountScore.CompareTo(right.MaximumAmountScore));
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && ELCPUtilities.UseELCPMultiThreading)
		{
			Diagnostics.Log("ELCP {0} with {1} initial score: {2}", new object[]
			{
				this.empire,
				empire,
				num3
			});
			foreach (AILayer_Diplomacy.TermEvaluation termEvaluation5 in this.availableTermEvaluationsCacheList)
			{
				Diagnostics.Log("ELCP {0} with {1} terms {2}, score {3}, other score: {4}", new object[]
				{
					this.empire,
					empire,
					termEvaluation5.Term.ToString(),
					termEvaluation5.MaximumAmountScore,
					(layer != null) ? layer.EvaluateDecision(termEvaluation5.Term, this.empire, null).Score.ToString() : "null"
				});
			}
		}
		float num19 = this.DiplomacyFocus ? (this.maximumContractPropositionEvaluationScore / 2f) : this.maximumContractPropositionEvaluationScore;
		Interval otherInterval = new Interval(this.minimumContractPropositionEvaluationScore, num19);
		Interval interval = new Interval(num3, num3);
		while (diplomaticTerms.Count < 7 && this.availableTermEvaluationsCacheList.Count > 0 && !interval.Intersect(otherInterval))
		{
			AILayer_Diplomacy.TermEvaluation termEvaluation6 = null;
			if (interval.IsGreaterThan(otherInterval))
			{
				for (int l = 0; l < this.availableTermEvaluationsCacheList.Count; l++)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation7 = this.availableTermEvaluationsCacheList[l];
					if (termEvaluation7.LowerScore <= 0f)
					{
						Interval interval2 = new Interval(interval.LowerBound + termEvaluation7.LowerScore, interval.UpperBound + termEvaluation7.GreaterScore);
						if (!interval2.IsLowerThan(otherInterval))
						{
							termEvaluation6 = termEvaluation7;
							break;
						}
					}
				}
			}
			else if (interval.IsLowerThan(otherInterval))
			{
				for (int m = this.availableTermEvaluationsCacheList.Count - 1; m >= 0; m--)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation8 = this.availableTermEvaluationsCacheList[m];
					if (termEvaluation8.GreaterScore >= 0f)
					{
						Interval interval3 = new Interval(interval.LowerBound + termEvaluation8.LowerScore, interval.UpperBound + termEvaluation8.GreaterScore);
						if (!interval3.IsGreaterThan(otherInterval))
						{
							termEvaluation6 = termEvaluation8;
							break;
						}
					}
				}
			}
			if (termEvaluation6 == null)
			{
				break;
			}
			diplomaticTerms.Add(termEvaluation6.Term);
			this.availableTermEvaluationsCacheList.Remove(termEvaluation6);
			interval = new Interval(interval.LowerBound + termEvaluation6.LowerScore, interval.UpperBound + termEvaluation6.GreaterScore);
		}
		if (interval.UpperBound > interval.LowerBound)
		{
			num3 = this.AnalyseContractProposition(diplomaticTerms, empire);
			if (num3 < this.minimumContractPropositionEvaluationScore || num3 > num19)
			{
				if (interval.UpperBound <= 0f)
				{
					for (int n = 0; n < diplomaticTerms.Count; n++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = diplomaticTerms[n] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange2 != null)
						{
							float num4;
							float amount;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange2, out num4, out amount);
							diplomaticTermResourceExchange2.Amount = amount;
						}
					}
					return;
				}
				if (interval.LowerBound >= 0f)
				{
					for (int num5 = 0; num5 < diplomaticTerms.Count; num5++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = diplomaticTerms[num5] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange3 != null)
						{
							float amount2;
							float num6;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange3, out amount2, out num6);
							diplomaticTermResourceExchange3.Amount = amount2;
						}
					}
					return;
				}
				if (interval.UpperBound > 0f && interval.LowerBound < 0f)
				{
					int num7 = 0;
					float num8 = 0f;
					while ((num3 < this.minimumContractPropositionEvaluationScore || num3 > num19) && num7 < diplomaticTerms.Count)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange4 = diplomaticTerms[num7] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange4 == null)
						{
							num7++;
						}
						else
						{
							float num9;
							float num10;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange4, out num9, out num10);
							float num11 = num10 - num9;
							DecisionResult decisionResult = this.EvaluateDecision(diplomaticTermResourceExchange4, empire, null);
							if ((num3 < this.minimumContractPropositionEvaluationScore && decisionResult.Score > 0f) || (num3 > num19 && decisionResult.Score < 0f))
							{
								if (num8 < 0f)
								{
									num7++;
									continue;
								}
								float num12 = Mathf.Max(0.1f * num11, 1f);
								float num13 = Mathf.Floor(diplomaticTermResourceExchange4.Amount + num12);
								if (num13 >= num10)
								{
									diplomaticTermResourceExchange4.Amount = num10;
									num7++;
									continue;
								}
								num8 = num12;
								diplomaticTermResourceExchange4.Amount = num13;
							}
							else if ((num3 < this.minimumContractPropositionEvaluationScore && decisionResult.Score < 0f) || (num3 > num19 && decisionResult.Score > 0f))
							{
								if (num8 > 0f)
								{
									num7++;
									continue;
								}
								float num14 = Mathf.Max(0.1f * num11, 1f);
								float num15 = Mathf.Floor(diplomaticTermResourceExchange4.Amount - num14);
								if (num15 <= num9)
								{
									diplomaticTermResourceExchange4.Amount = num9;
									num7++;
									continue;
								}
								num8 = -num14;
								diplomaticTermResourceExchange4.Amount = num15;
							}
							num3 = this.AnalyseContractProposition(diplomaticTerms, empire);
						}
					}
				}
			}
		}
		foreach (DiplomaticTerm diplomaticTerm3 in diplomaticTerms)
		{
			if (diplomaticTerm3 is DiplomaticTermBoosterExchange)
			{
				foreach (GameEntityGUID gameEntityGUID in (diplomaticTerm3 as DiplomaticTermBoosterExchange).BoosterGUID)
				{
					this.ResourceManager.BoostersInUse.Add(gameEntityGUID.ToString());
				}
			}
		}
	}

	private void FillInterpreterContext(global::Empire otherEmpire, InterpreterContext interpreterContext)
	{
		Diagnostics.Assert(otherEmpire.Index != this.empire.Index);
		if (this.lastEmpireInInterpreter == otherEmpire)
		{
			return;
		}
		this.lastEmpireInInterpreter = otherEmpire;
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(otherEmpire);
		Diagnostics.Assert(agentGroupForEmpire != null);
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		Diagnostics.Assert(service != null);
		AILayer_Attitude layer = base.AIEntity.GetLayer<AILayer_Attitude>();
		AILayer_Attitude.Attitude attitude = layer.GetAttitude(otherEmpire);
		Agent agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.WarTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("WarTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.TruceTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("TruceTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.ColdWarTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("ColdWarTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.PeaceTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("PeaceTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.AllianceTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("AllianceTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = this.aiLayerDiplomacyAmas.Amas.GetAgent(AILayer_DiplomacyAmas.AgentNames.GlobalWarTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("GlobalWarTermAgentCriticity", agent.CriticityMax.Intensity);
		Diagnostics.Assert(this.aiLayerResourceAmas != null);
		agent = this.aiLayerResourceAmas.Amas.GetAgent(AILayer_ResourceAmas.AgentNames.MoneyReferenceRatio);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MoneyReferenceRatioAgentCriticity", agent.CriticityMax.Intensity);
		agent = this.aiLayerResourceAmas.Amas.GetAgent(AILayer_ResourceAmas.AgentNames.TechnologyReferenceTurnCount);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("TechnologyReferenceTurnCountAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.MapExchangeTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MapExchangeTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.MapEmbargoTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MapEmbargoTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.VisionAndMapExchangeTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("VisionAndMapExchangeTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.VisionAndMapEmbargoTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("VisionAndMapEmbargoTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.VisionEmbargoTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("VisionEmbargoTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.CommercialAgreementTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("CommercialAgreementTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.ResearchAgreementTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("ResearchAgreementTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.CloseBordersTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("CloseBordersTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.OpenBordersTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("OpenBordersTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.MarketBanTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MarketBanTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.MarketBanNullificationTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MarketBanNullificationTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.MarketBanRemovalTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("MarketBanRemovalTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.BlackSpotTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("BlackSpotTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.BlackSpotRemovalTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("BlackSpotRemovalTermAgentCriticity", agent.CriticityMax.Intensity);
		agent = agentGroupForEmpire.GetAgent(AILayer_DiplomacyAmas.AgentNames.BlackSpotNullificationTermAgent);
		Diagnostics.Assert(agent != null && agent.CriticityMax != null);
		interpreterContext.Register("BlackSpotNullificationTermAgentCriticity", agent.CriticityMax.Intensity);
		float worldExplorationRatio = service.GetWorldExplorationRatio(this.empire);
		interpreterContext.Register("WorldExplorationRatio", worldExplorationRatio);
		float commonBorderRatio = service.GetCommonBorderRatio(this.empire, otherEmpire);
		interpreterContext.Register("CommonBorderRatio", commonBorderRatio);
		float worldColonizationRatio = service.GetWorldColonizationRatio(otherEmpire);
		interpreterContext.Register("OtherEmpireWorldColonizationRatio", worldColonizationRatio);
		float scoreByName = attitude.Score.GetScoreByName(AILayer_Attitude.AttitudeScoreDefinitionReferences.UnitsInMyTerritory);
		interpreterContext.Register("AttitudeScoreUnitsInMyTerritory", scoreByName);
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			float propertyValue = this.majorEmpires[i].GetPropertyValue(SimulationProperties.NetEmpirePoint);
			num = ((propertyValue <= num) ? num : propertyValue);
			propertyValue = this.majorEmpires[i].GetPropertyValue(SimulationProperties.NetEmpireMoney);
			num2 = ((propertyValue <= num2) ? num2 : propertyValue);
		}
		interpreterContext.Register("BestEmpireNetEmpirePointValue", num);
		interpreterContext.Register("BestEmpireNetEmpireMoneyValue", num2);
	}

	private float GetAgentCriticityFor(MajorEmpire opponentEmpire, StaticString agentName)
	{
		AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
		if (agentGroupForEmpire == null)
		{
			return 0f;
		}
		Agent agent = agentGroupForEmpire.GetAgent(agentName);
		if (agent == null)
		{
			return 0f;
		}
		return agent.CriticityMax.Intensity;
	}

	private void GetDiplomaticTermAmountLimits(DiplomaticTerm diplomaticTerm, out float minimumAmount, out float maximumAmount)
	{
		DepartmentOfForeignAffairs.GetDiplomaticTermAmountLimits(diplomaticTerm, out minimumAmount, out maximumAmount);
		float num = maximumAmount - minimumAmount;
		maximumAmount = minimumAmount + this.maximumExchangeAmountRatio * num;
		minimumAmount += this.minimumExchangeAmountRatio * num;
	}

	private StaticString GetMostWantedDiplomaticTermAgentName(MajorEmpire opponentEmpire)
	{
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		Agent agent = null;
		Diagnostics.Assert(this.diplomaticRelationStateByAmasAgentName != null);
		foreach (KeyValuePair<StaticString, DiplomaticTermDefinition[]> keyValuePair in this.diplomaticTermByAmasAgentName)
		{
			Agent agent2 = agentGroupForEmpire.GetAgent(keyValuePair.Key);
			Diagnostics.Assert(agent2 != null);
			bool flag = false;
			foreach (DiplomaticTermDefinition constructibleElement in keyValuePair.Value)
			{
				if (DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, this.empire, opponentEmpire, constructibleElement, new string[0]))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				if (agent == null || agent2.CriticityMax.Intensity > agent.CriticityMax.Intensity)
				{
					agent = agent2;
				}
			}
		}
		if (agent == null || agent.CriticityMax.Intensity < this.diplomaticTermAgentCriticityThreshold)
		{
			return null;
		}
		Diagnostics.Assert(agent.AgentDefinition != null && agent.AgentDefinition.Name != null);
		Diagnostics.Assert(this.diplomaticTermByAmasAgentName.ContainsKey(agent.AgentDefinition.Name));
		return agent.AgentDefinition.Name;
	}

	private DiplomaticRelationState GetWantedDiplomaticRelationStateWith(MajorEmpire opponentEmpire, out float criticity)
	{
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
		Agent agent = null;
		Diagnostics.Assert(this.diplomaticRelationStateByAmasAgentName != null);
		foreach (KeyValuePair<StaticString, DiplomaticRelationState> keyValuePair in this.diplomaticRelationStateByAmasAgentName)
		{
			Agent agent2 = agentGroupForEmpire.GetAgent(keyValuePair.Key);
			Diagnostics.Assert(agent2 != null && agent2.CriticityMax != null);
			Diagnostics.Assert(keyValuePair.Value != null);
			StaticString name = keyValuePair.Value.Name;
			if (agent2.Enable)
			{
				if (agent == null)
				{
					agent = agent2;
				}
				else
				{
					Diagnostics.Assert(agent.CriticityMax != null);
					if (Math.Abs(agent2.CriticityMax.Intensity - agent.CriticityMax.Intensity) < this.diplomaticRelationStateAgentCriticityEpsilon)
					{
						int num = this.DistanceToCurrentDiplomaticRelationState(opponentEmpire, this.diplomaticRelationStateByAmasAgentName[agent.Name].Name);
						if (this.DistanceToCurrentDiplomaticRelationState(opponentEmpire, name) < num)
						{
							agent = agent2;
						}
					}
					else if (agent2.CriticityMax.Intensity > agent.CriticityMax.Intensity)
					{
						agent = agent2;
					}
				}
			}
		}
		criticity = agent.CriticityMax.Intensity;
		Diagnostics.Assert(agent != null);
		if (agent.CriticityMax.Intensity < this.diplomaticRelationStateAgentCriticityThreshold)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
			Diagnostics.Assert(diplomaticRelation != null);
			return diplomaticRelation.State;
		}
		Diagnostics.Assert(agent.AgentDefinition != null && agent.AgentDefinition.Name != null);
		Diagnostics.Assert(this.diplomaticRelationStateByAmasAgentName.ContainsKey(agent.AgentDefinition.Name));
		return this.diplomaticRelationStateByAmasAgentName[agent.AgentDefinition.Name];
	}

	private void InitializeWishes()
	{
		this.diplomaticTermEvaluator = new ElementEvaluator<DiplomaticTerm, InterpreterContext>(this, new InterpreterContext(null));
		this.diplomaticTermEvaluator.BeginEvaluationDelegate = new ElementEvaluator<DiplomaticTerm, InterpreterContext>.BeginEvaluationFunc(this.BeginElementEvaluationDelegate);
		this.diplomaticTermEvaluator.FinalInterestDelegate = new ElementEvaluator<DiplomaticTerm, InterpreterContext>.InterestFunc(this.FinalInterestDelegate);
		this.diplomaticTermEvaluator.IsOutputValidDelegate = new ElementEvaluator<DiplomaticTerm, InterpreterContext>.OutputActivationPrerequisiteFunc(this.IsOuputValidForElementDelegate);
		this.diplomaticTermEvaluator.ContextWeightDelegate = new ElementEvaluator<DiplomaticTerm, InterpreterContext>.ContextWeightFunc(this.ContextWeightDelegate);
		this.diplomaticTermEvaluator.RegisterOutputs(typeof(AILayer_Diplomacy), new string[]
		{
			"DiplomacyUtilityProvider",
			"DiplomacyUtilityReceiver",
			"DiplomacyEconomyProvider",
			"DiplomacyEconomyReceiver",
			"DiplomacyTechnologyProvider",
			"DiplomacyTechnologyReceiver",
			"DiplomacyMilitarySupportProvider",
			"DiplomacyMilitarySupportReceiver",
			"DiplomacyOrbProvider",
			"DiplomacyOrbReceiver"
		});
	}

	private float ContextWeightDelegate(DiplomaticTerm aiEvaluableElement, InterpreterContext context, StaticString outputName, AIHeuristicAnalyser.Context debugContext)
	{
		float num = 1f;
		global::Empire empire = (aiEvaluableElement.EmpireWhichProvides.Index != this.empire.Index) ? aiEvaluableElement.EmpireWhichProvides : aiEvaluableElement.EmpireWhichReceives;
		float num2 = 0f;
		AILayer_Attitude.Attitude attitude = this.aiLayerAttitude.GetAttitude(empire);
		float scoreByCategory = attitude.Score.GetScoreByCategory(AILayer_Attitude.Attitude.Category.Envy);
		float scoreByCategory2 = attitude.Score.GetScoreByCategory(AILayer_Attitude.Attitude.Category.Fear);
		float scoreByCategory3 = attitude.Score.GetScoreByCategory(AILayer_Attitude.Attitude.Category.Trust);
		float scoreByCategory4 = attitude.Score.GetScoreByCategory(AILayer_Attitude.Attitude.Category.War);
		float num3 = Mathf.Clamp(-scoreByCategory, -100f, 100f) / 100f;
		float num4 = Mathf.Clamp(scoreByCategory2, -100f, 100f) / 100f;
		float num5 = Mathf.Clamp(scoreByCategory3, -100f, 100f) / 100f;
		float num6 = Mathf.Clamp(-scoreByCategory4, -100f, 100f) / 100f;
		string text = outputName;
		if (text != null)
		{
			if (AILayer_Diplomacy.<>f__switch$map4 == null)
			{
				AILayer_Diplomacy.<>f__switch$map4 = new Dictionary<string, int>(6)
				{
					{
						"DiplomacyEconomyProvider",
						0
					},
					{
						"DiplomacyOrbProvider",
						0
					},
					{
						"DiplomacyTechnologyProvider",
						0
					},
					{
						"DiplomacyEconomyReceiver",
						1
					},
					{
						"DiplomacyOrbReceiver",
						1
					},
					{
						"DiplomacyTechnologyReceiver",
						1
					}
				};
			}
			int num7;
			if (AILayer_Diplomacy.<>f__switch$map4.TryGetValue(text, out num7))
			{
				if (num7 != 0)
				{
					if (num7 == 1)
					{
						num2 = num5 + num4 - num6 - num3;
					}
				}
				else
				{
					num2 = num6 + num3 - num5 - num4;
				}
			}
		}
		float num8 = Mathf.Clamp(1f + num2, 0.5f, 1.5f);
		num2 = num8;
		return num * num2;
	}

	private bool IsOuputValidForElementDelegate(DiplomaticTerm diplomaticTerm, StaticString outputName)
	{
		if (diplomaticTerm.EmpireWhichProvides.Index == this.empire.Index)
		{
			return outputName == "DiplomacyUtilityProvider" || outputName == "DiplomacyEconomyProvider" || outputName == "DiplomacyTechnologyProvider" || outputName == "DiplomacyMilitarySupportProvider" || outputName == "DiplomacyOrbProvider";
		}
		return diplomaticTerm.EmpireWhichReceives.Index == this.empire.Index && (outputName == "DiplomacyUtilityReceiver" || outputName == "DiplomacyEconomyReceiver" || outputName == "DiplomacyTechnologyReceiver" || outputName == "DiplomacyMilitarySupportReceiver" || outputName == "DiplomacyOrbReceiver");
	}

	private float FinalInterestDelegate(DiplomaticTerm diplomaticTerm, InterpreterContext context, float globalInterestValue, AIHeuristicAnalyser.Context debugContext)
	{
		if (diplomaticTerm == null)
		{
			throw new ArgumentNullException("diplomaticTerm");
		}
		float propertyValue = this.empire.GetPropertyValue(SimulationProperties.EmpirePointStock);
		float b = propertyValue + this.empire.GetPropertyValue(SimulationProperties.NetEmpirePoint) * this.empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier) * 8f;
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
		return globalInterestValue - empirePointCost / Mathf.Max(1f, b);
	}

	public TickableState State { get; set; }

	public StaticString GetMostWantedDiplomaticTermAgentName(global::Empire opponentEmpire)
	{
		if (opponentEmpire == null)
		{
			throw new ArgumentNullException("opponentEmpire");
		}
		Diagnostics.Assert(this.mostWantedDiplomaticTermAgentNameByEmpireIndex != null);
		return this.mostWantedDiplomaticTermAgentNameByEmpireIndex[opponentEmpire.Index];
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		Diagnostics.Assert(aiEntity != null);
		this.diplomaticRelationStateDatabase = Databases.GetDatabase<DiplomaticRelationState>(false);
		this.contructibleElementDatabase = Databases.GetDatabase<DepartmentOfForeignAffairs.ConstructibleElement>(false);
		this.aiParameterDatabase = Databases.GetDatabase<AIParameterDatatableElement>(false);
		this.aiParameterConverterDatabase = Databases.GetDatabase<AIParameterConverter>(false);
		this.eventService = Services.GetService<IEventService>();
		Diagnostics.Assert(this.eventService != null);
		this.eventService.EventRaise += this.EventService_EventRaise;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.tickableRepositoryAIHelper = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.tickableRepositoryAIHelper.Register(this);
		this.game = (service.Game as global::Game);
		Diagnostics.Assert(this.game != null && this.game.Empires != null);
		this.majorEmpires = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>(this.game.Empires, (global::Empire empire) => empire is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
		this.mostWantedDiplomaticTermAgentNameByEmpireIndex = new StaticString[this.majorEmpires.Length];
		this.empire = aiEntity.Empire;
		Diagnostics.Assert(this.empire != null);
		this.departmentOfForeignAffairs = this.empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.departmentOfForeignAffairs != null);
		this.departmentOfScience = this.empire.GetAgency<DepartmentOfScience>();
		Diagnostics.Assert(this.departmentOfScience != null);
		this.departmentOfPlanificationAndDevelopment = this.empire.GetAgency<DepartmentOfPlanificationAndDevelopment>();
		Diagnostics.Assert(this.departmentOfPlanificationAndDevelopment != null);
		this.departmentOfTheInterior = this.empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfEducation = this.empire.GetAgency<DepartmentOfEducation>();
		this.departmentOfTheTreasury = this.empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfDefense = this.empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		this.diplomacyService = this.game.Services.GetService<IDiplomacyService>();
		this.diplomacyContractRepositoryService = this.game.Services.GetService<IDiplomaticContractRepositoryService>();
		this.gameEntityRepositoryService = this.game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.worldPositionningService = this.game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		Diagnostics.Assert(aiEntity.AIPlayer != null);
		AIEntity entity = aiEntity.AIPlayer.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(entity != null);
		this.aiLayerDiplomacyAmas = entity.GetLayer<AILayer_DiplomacyAmas>();
		this.aiLayerResourceAmas = entity.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		this.aiLayerAccountManager = aiEntity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		this.aiLayerResearch = base.AIEntity.GetLayer<AILayer_Research>();
		Diagnostics.Assert(this.aiLayerResearch != null);
		this.aiLayerAttitude = aiEntity.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		this.aiLayerNavy = aiEntity.GetLayer<AILayer_Navy>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		DepartmentOfForeignAffairs.ConstructibleElement constructibleElement;
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.Warning, out constructibleElement))
		{
			this.diplomaticTermWarning = (constructibleElement as DiplomaticTermDefinition);
		}
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.Gratify, out constructibleElement))
		{
			this.diplomaticTermGratify = (constructibleElement as DiplomaticTermDefinition);
		}
		Diagnostics.Assert(this.diplomaticTermWarning != null);
		Diagnostics.Assert(this.diplomaticTermGratify != null);
		this.InitializeData();
		this.InitializeDiplomaticTermEvaluationHelper();
		this.InitializeWishes();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Diplomacy_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Diplomacy_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Diplomacy_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		IPersonalityAIHelper service2 = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.maximumNumberDiplomaticProposalsPerTurn = service2.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", "AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy", "MaximumNumberDiplomaticProposalsPerTurn"), 1f);
		this.SweetenDealThreshold = service2.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", "AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy", "SweetenDealThreshold"), 2f);
		this.EmergencyGangupLimit = service2.GetRegistryValue<float>(base.AIEntity.Empire, string.Format("{0}/{1}", "AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy", "EmergencyGangupLimit"), 1f);
		this.AlreadyContactedEmpires = new List<int>();
		ISessionService service3 = Services.GetService<ISessionService>();
		Diagnostics.Assert(service != null);
		this.VictoryLayer = base.AIEntity.GetLayer<AILayer_Victory>();
		this.ResourceManager = base.AIEntity.GetLayer<AILayer_ResourceManager>();
		this.SharedVictory = service3.Session.GetLobbyData<bool>("Shared", true);
		this.canInfect = base.AIEntity.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitMimics3);
		this.canNeverDeclareWar = DepartmentOfTheInterior.CanNeverDeclareWar(this.empire);
		string lobbyData = service3.Session.GetLobbyData<string>("GameDifficulty", "Serious");
		if (lobbyData == "Newbie")
		{
			this.GameDifficulty = 0;
		}
		else if (lobbyData == "Easy")
		{
			this.GameDifficulty = 1;
		}
		else if (lobbyData == "Normal")
		{
			this.GameDifficulty = 2;
		}
		else if (lobbyData == "Hard")
		{
			this.GameDifficulty = 3;
		}
		else if (lobbyData == "Serious")
		{
			this.GameDifficulty = 4;
		}
		else if (lobbyData == "Impossible")
		{
			this.GameDifficulty = 5;
		}
		else if (lobbyData == "Endless")
		{
			this.GameDifficulty = 6;
		}
		AILayer_Diplomacy.VictoryTargets[this.empire.Index] = -1;
		GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
		this.aIScheduler = gameServer.AIScheduler;
		Diagnostics.Assert(this.aIScheduler != null);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		if (this.eventService != null)
		{
			this.eventService.EventRaise -= this.EventService_EventRaise;
			this.eventService = null;
		}
		if (this.departmentOfForeignAffairs != null)
		{
			this.departmentOfForeignAffairs.DiplomaticRelationStateChange -= this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
			this.departmentOfForeignAffairs = null;
		}
		if (this.tickableRepositoryAIHelper != null)
		{
			this.tickableRepositoryAIHelper.Unregister(this);
			this.tickableRepositoryAIHelper.UnregisterUpdate(this);
			this.tickableRepositoryAIHelper = null;
		}
		AILayer_Diplomacy.VictoryTargets[this.empire.Index] = -1;
		this.empire = null;
		this.departmentOfScience = null;
		this.departmentOfPlanificationAndDevelopment = null;
		this.departmentOfTheInterior = null;
		this.departmentOfEducation = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfDefense = null;
		this.diplomacyService = null;
		this.diplomacyContractRepositoryService = null;
		this.gameEntityRepositoryService = null;
		this.worldPositionningService = null;
		this.aiLayerDiplomacyAmas = null;
		this.aiLayerResourceAmas = null;
		this.aiLayerAccountManager = null;
		this.aiLayerResearch = null;
		this.aiLayerAttitude = null;
		this.aiLayerNavy = null;
		this.majorEmpires = null;
		this.game = null;
		this.mostWantedDiplomaticTermAgentNameByEmpireIndex = new StaticString[0];
		this.diplomaticRelationStateDatabase = null;
		this.contructibleElementDatabase = null;
		this.aiParameterDatabase = null;
		this.aiParameterConverterDatabase = null;
		this.VictoryLayer = null;
		this.ResourceManager = null;
		this.coroutineActive = false;
		this.currentContractCoroutine = null;
		this.currentDiplomaticTerms.Clear();
		this.diplomaticRelationStateByAmasAgentName.Clear();
		this.AlreadyContactedEmpires.Clear();
		this.aIScheduler = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		this.tickableRepositoryAIHelper.UnregisterUpdate(this);
		this.coroutineActive = false;
		this.currentContractCoroutine = null;
		this.DiplomacyFocus = (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Diplomacy);
		float num = (float)this.departmentOfTheInterior.Cities.Count;
		int currentTechnologyEraNumber = this.departmentOfScience.CurrentTechnologyEraNumber;
		float num2 = num * (float)currentTechnologyEraNumber * this.diplomaticMaximumAccountMultiplier;
		float num3 = 0.25f;
		if (this.DiplomacyFocus)
		{
			num3 = 0.5f;
		}
		float num4;
		base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num4, false);
		if (num2 < num4 * num3)
		{
			num2 = num4 * num3;
		}
		base.AIEntity.GetLayer<AILayer_AccountManager>().SetMaximalAccount(AILayer_AccountManager.DiplomacyAccountName, num2);
		this.ClearContractRequests();
		Blackboard blackboard = base.AIEntity.AIPlayer.Blackboard;
		this.GetMilitaryPowerDif(false);
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			this.PeaceWish[majorEmpire.Index] = false;
			if (majorEmpire.Index != this.empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				if (diplomaticRelation.State != null && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead))
				{
					float criticity;
					DiplomaticRelationState wantedDiplomaticRelationStateWith = this.GetWantedDiplomaticRelationStateWith(majorEmpire, out criticity);
					if (wantedDiplomaticRelationStateWith.Name == DiplomaticRelationState.Names.Peace || wantedDiplomaticRelationStateWith.Name == DiplomaticRelationState.Names.Alliance)
					{
						this.PeaceWish[majorEmpire.Index] = true;
					}
					if (blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == majorEmpire.Index) == null)
					{
						blackboard.AddMessage(new WantedDiplomaticRelationStateMessage
						{
							TimeOut = this.wantedDiplomaticRelationStateMessageRevaluationPeriod,
							WantedDiplomaticRelationStateName = wantedDiplomaticRelationStateWith.Name,
							OpponentEmpireIndex = majorEmpire.Index,
							Criticity = criticity
						});
					}
					Diagnostics.Assert(this.mostWantedDiplomaticTermAgentNameByEmpireIndex != null);
					this.mostWantedDiplomaticTermAgentNameByEmpireIndex[majorEmpire.Index] = this.GetMostWantedDiplomaticTermAgentName(majorEmpire);
				}
			}
		}
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		this.WantedPrestigePoint = 0f;
		Diagnostics.Assert(this.majorEmpires != null);
		this.AlreadyContactedEmpires.Clear();
		this.DealIndeces.Clear();
		this.AnyVictoryreactionNeeded = false;
		this.opportunistThisTurn = false;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			Diagnostics.Assert(majorEmpire != null);
			AIPlayer_MajorEmpire aiplayer_MajorEmpire;
			if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(majorEmpire, out aiplayer_MajorEmpire))
			{
				bool flag = aiplayer_MajorEmpire.AIState == AIPlayer.PlayerState.EmpireControlledByHuman;
				if (majorEmpire.Index != this.empire.Index && this.NeedsVictoryReaction[majorEmpire.Index])
				{
					if ((this.SharedVictory || (this.GameDifficulty < 4 && flag)) && this.departmentOfForeignAffairs.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.Alliance)
					{
						this.NeedsVictoryReaction[majorEmpire.Index] = false;
					}
					else if ((this.GameDifficulty < 3 && flag && this.departmentOfForeignAffairs.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.Peace) || this.departmentOfForeignAffairs.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.Dead)
					{
						this.NeedsVictoryReaction[majorEmpire.Index] = false;
					}
					else
					{
						this.AnyVictoryreactionNeeded = true;
					}
				}
			}
		}
		this.empire.Refresh(true);
		for (int j = 0; j < this.majorEmpires.Length; j++)
		{
			MajorEmpire majorEmpire2 = this.majorEmpires[j];
			Diagnostics.Assert(majorEmpire2 != null);
			if (majorEmpire2.Index != this.empire.Index && !majorEmpire2.IsEliminated)
			{
				this.GenerateContractRequests(majorEmpire2);
			}
		}
		if (this.PrisonDealIndeces.Count > 0)
		{
			for (int k = 0; k < this.majorEmpires.Length; k++)
			{
				MajorEmpire majorEmpire3 = this.majorEmpires[k];
				if (majorEmpire3.Index != this.empire.Index && this.ContractRequests.Count == 0 && this.PrisonDealIndeces.Contains(majorEmpire3.Index))
				{
					this.GeneratePrisonerDeal(majorEmpire3);
				}
			}
			this.PrisonDealIndeces.Clear();
		}
		if (this.DealIndeces.Count > 0 || this.MimicCityDealIndeces.Count > 0)
		{
			for (int l = 0; l < this.majorEmpires.Length; l++)
			{
				MajorEmpire majorEmpire4 = this.majorEmpires[l];
				if (majorEmpire4.Index != this.empire.Index && this.ContractRequests.Count == 0)
				{
					if (this.MimicCityDealIndeces.Contains(majorEmpire4.Index))
					{
						this.GenerateSymbiosisRequest(majorEmpire4);
					}
					if (this.DealIndeces.Contains(majorEmpire4.Index) && this.ContractRequests.Count == 0)
					{
						if (this.DiplomacyFocus)
						{
							this.GenerateTechRequest(majorEmpire4);
							if (this.ContractRequests.Count == 0)
							{
								this.GenerateResourceRequest(majorEmpire4);
							}
						}
						else
						{
							this.GenerateResourceRequest(majorEmpire4);
							if (this.ContractRequests.Count == 0)
							{
								this.GenerateTechRequest(majorEmpire4);
							}
						}
					}
				}
			}
			this.MimicCityDealIndeces.Clear();
			this.DealIndeces.Clear();
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.currentContractRequest = null;
		float num = this.maximumNumberDiplomaticProposalsPerTurn;
		int i = 0;
		while (i < this.ContractRequests.Count)
		{
			if (num < 1f && num > 0f)
			{
				if ((int)((float)this.game.Turn % (1f / num)) == 0)
				{
					this.ContractRequests[i].State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
					break;
				}
				break;
			}
			else
			{
				if (num < 1f)
				{
					break;
				}
				this.ContractRequests[i].State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				num -= 1f;
				i++;
			}
		}
		this.State = TickableState.NeedTick;
	}

	private void DepartmentOfForeignAffairs_DiplomaticRelationStateChange(object sender, DiplomaticRelationStateChangeEventArgs e)
	{
		Blackboard blackboard = base.AIEntity.AIPlayer.Blackboard;
		WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == e.EmpireWithWhichTheStatusChange.Index);
		if (wantedDiplomaticRelationStateMessage == null)
		{
			wantedDiplomaticRelationStateMessage = new WantedDiplomaticRelationStateMessage();
			blackboard.AddMessage(wantedDiplomaticRelationStateMessage);
		}
		wantedDiplomaticRelationStateMessage.TimeOut = this.wantedDiplomaticRelationStateMessageRevaluationPeriod;
		wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName = e.DiplomaticRelationState.Name;
		wantedDiplomaticRelationStateMessage.OpponentEmpireIndex = e.EmpireWithWhichTheStatusChange.Index;
		wantedDiplomaticRelationStateMessage.Criticity = 1f;
		this.RelationStateChanged(e);
	}

	private void EventService_EventRaise(object sender, EventRaiseEventArgs e)
	{
		if (base.AIEntity == null || base.AIEntity.AIPlayer == null || base.AIEntity.AIPlayer.AIState != AIPlayer.PlayerState.EmpireControlledByAI)
		{
			return;
		}
		EventDiplomaticContractStateChange eventDiplomaticContractStateChange = e.RaisedEvent as EventDiplomaticContractStateChange;
		if (eventDiplomaticContractStateChange != null && eventDiplomaticContractStateChange.DiplomaticContract.EmpireWhichReceives.Index == this.empire.Index && eventDiplomaticContractStateChange.DiplomaticContract.State == DiplomaticContractState.Proposed)
		{
			this.AnswerContractProposition(eventDiplomaticContractStateChange.DiplomaticContract);
			return;
		}
		this.OnVictoryConditionAlertEventRaise(e.RaisedEvent);
	}

	private void InitializeData()
	{
		Diagnostics.Assert(this.diplomaticRelationStateDatabase != null);
		Diagnostics.Assert(this.diplomaticRelationStateByAmasAgentName != null);
		DiplomaticRelationState value;
		if (this.diplomaticRelationStateDatabase.TryGetValue(DiplomaticRelationState.Names.War, out value))
		{
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.WarTermAgent, value);
		}
		if (this.diplomaticRelationStateDatabase.TryGetValue(DiplomaticRelationState.Names.Truce, out value))
		{
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.TruceTermAgent, value);
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ForceTruceTermAgent, value);
		}
		if (this.diplomaticRelationStateDatabase.TryGetValue(DiplomaticRelationState.Names.ColdWar, out value))
		{
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ColdWarTermAgent, value);
		}
		if (this.diplomaticRelationStateDatabase.TryGetValue(DiplomaticRelationState.Names.Peace, out value))
		{
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.PeaceTermAgent, value);
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ForcePeaceTermAgent, value);
		}
		if (this.diplomaticRelationStateDatabase.TryGetValue(DiplomaticRelationState.Names.Alliance, out value))
		{
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.AllianceTermAgent, value);
			this.diplomaticRelationStateByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ForceAllianceTermAgent, value);
		}
		List<DiplomaticTermDefinition> list = new List<DiplomaticTermDefinition>();
		Diagnostics.Assert(this.contructibleElementDatabase != null);
		Diagnostics.Assert(this.diplomaticTermByAmasAgentName != null);
		DepartmentOfForeignAffairs.ConstructibleElement constructibleElement;
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MapExchange, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MapExchangeTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.VisionAndMapExchange, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.VisionAndMapExchangeTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.CloseBorders, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.CloseBordersTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.OpenBorders, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.OpenBordersTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MapEmbargo, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MapEmbargoTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.VisionEmbargo, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.VisionEmbargoTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.VisionAndMapEmbargo, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.VisionAndMapEmbargoTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.CommercialAgreement, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.CommercialAgreementTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.ResearchAgreement, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ResearchAgreementTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.CommercialEmbargo, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.CommercialEmbargoTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.ResearchEmbargo, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.ResearchEmbargoTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MarketBan, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MarketBanTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MarketBanRemoval, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MarketBanRemovalTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MarketBanNullification, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MarketBanNullificationTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MoneyPrint, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MoneyPrintTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.MoneyPrintNullification, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.MoneyPrintNullificationTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.BlackSpot, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.BlackSpotTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.BlackSpotNullification, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.BlackSpotNullificationTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.BlackSpotRemoval, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.diplomaticTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.BlackSpotRemovalTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.AskToDeclareWar, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermProposalDefinition);
			DiplomaticTermProposalDefinition diplomaticTermProposalDefinition = constructibleElement as DiplomaticTermProposalDefinition;
			if (diplomaticTermProposalDefinition != null && diplomaticTermProposalDefinition.AllowedTreaties != null && diplomaticTermProposalDefinition.AllowedTreaties.Length > 0)
			{
				DepartmentOfForeignAffairs.ConstructibleElement value2 = this.contructibleElementDatabase.GetValue(diplomaticTermProposalDefinition.AllowedTreaties[0]);
				DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = value2 as DiplomaticTermDiplomaticRelationStateDefinition;
				if (diplomaticTermDiplomaticRelationStateDefinition != null && diplomaticTermDiplomaticRelationStateDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.War)
				{
					list.Add(diplomaticTermProposalDefinition);
				}
			}
		}
		this.proposalTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.AskToBlackSpot, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermProposalDefinition);
			DiplomaticTermProposalDefinition diplomaticTermProposalDefinition2 = constructibleElement as DiplomaticTermProposalDefinition;
			if (diplomaticTermProposalDefinition2 != null && diplomaticTermProposalDefinition2.AllowedTreaties != null && diplomaticTermProposalDefinition2.AllowedTreaties.Length > 0)
			{
				DepartmentOfForeignAffairs.ConstructibleElement value3 = this.contructibleElementDatabase.GetValue(diplomaticTermProposalDefinition2.AllowedTreaties[0]);
				if (value3 != null && value3.Name == DiplomaticTermDefinition.Names.BlackSpot)
				{
					list.Add(diplomaticTermProposalDefinition2);
				}
			}
		}
		this.proposalTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.AskToBlackSpotTermAgent, list.ToArray());
		list.Clear();
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.PrisonerExchange, out constructibleElement))
		{
			Diagnostics.Assert(constructibleElement is DiplomaticTermDefinition);
			list.Add(constructibleElement as DiplomaticTermDefinition);
		}
		this.proposalTermByAmasAgentName.Add(AILayer_DiplomacyAmas.AgentNames.PrisonerExchange, list.ToArray());
		list.Clear();
	}

	private SynchronousJobState SynchronousJob_BeginTurn()
	{
		return SynchronousJobState.Success;
	}

	private void TryGenerateForceRequest(MajorEmpire opponentEmpire, StaticString wantedDiplomaticRelationState)
	{
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = null;
		foreach (DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition2 in this.departmentOfForeignAffairs.GetDiplomaticTermDiplomaticRelationStateDefinition(diplomaticContract, wantedDiplomaticRelationState))
		{
			Diagnostics.Assert(diplomaticTermDiplomaticRelationStateDefinition2 != null);
			if (diplomaticTermDiplomaticRelationStateDefinition2.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				diplomaticTermDiplomaticRelationStateDefinition = diplomaticTermDiplomaticRelationStateDefinition2;
			}
		}
		Agent forceStatusAgentFromDiplomaticRelationState = this.GetForceStatusAgentFromDiplomaticRelationState(opponentEmpire, wantedDiplomaticRelationState);
		if (diplomaticTermDiplomaticRelationStateDefinition != null && forceStatusAgentFromDiplomaticRelationState != null && forceStatusAgentFromDiplomaticRelationState.Enable && (forceStatusAgentFromDiplomaticRelationState.CriticityMax.Intensity >= this.diplomaticRelationStateAgentCriticityThreshold || this.AlwaysProcess))
		{
			DiplomaticTerm diplomaticTerm = new DiplomaticTermDiplomaticRelationState(diplomaticTermDiplomaticRelationStateDefinition, this.empire, this.empire, opponentEmpire);
			float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
			this.WantedPrestigePoint = Mathf.Max(empirePointCost, this.WantedPrestigePoint);
			float num = 0f;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, false);
			if (empirePointCost <= num)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
				if (this.AlwaysProcess)
				{
					contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				}
				contractRequest.Terms.Add(diplomaticTerm);
				this.ContractRequests.Add(contractRequest);
				return;
			}
		}
	}

	private void SweetenDeal(DiplomaticContract diplomaticContract, ref List<DiplomaticTerm> diplomaticTerms, global::Empire empire, AILayer_Diplomacy layer)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		if (diplomaticTerms == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		float num = 1f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(empire))
		{
			num = 2f;
		}
		else if (this.MilitaryPowerDif < 0f)
		{
			foreach (DiplomaticTerm diplomaticTerm in diplomaticTerms)
			{
				DiplomaticRelationStateTermDefinition diplomaticRelationStateTermDefinition = diplomaticTerm.Definition as DiplomaticRelationStateTermDefinition;
				if (diplomaticRelationStateTermDefinition != null && (diplomaticRelationStateTermDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.Peace || diplomaticRelationStateTermDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.Alliance))
				{
					float num2 = Mathf.Min(1f, Mathf.Abs(this.MilitaryPowerDif) / this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower));
					num += num2;
					break;
				}
			}
		}
		DiplomaticTermProposal diplomaticTermProposal = diplomaticTerms.Find((DiplomaticTerm y) => y is DiplomaticTermProposal && y.EmpireWhichProvides.Index == empire.Index) as DiplomaticTermProposal;
		if (diplomaticTermProposal != null && (this.departmentOfForeignAffairs.IsAtWarWith(diplomaticTermProposal.ChosenEmpire) || this.NeedsVictoryReaction[diplomaticTermProposal.ChosenEmpireIndex]))
		{
			num *= 2f;
		}
		if (this.DiplomacyFocus)
		{
			num *= 1.5f;
		}
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		float num3 = this.AnalyseContractProposition(diplomaticTerms, empire);
		float num4 = layer.AnalyseContractProposition(diplomaticTerms, this.empire);
		List<DiplomaticTerm> list2 = new List<DiplomaticTerm>();
		bool flag = false;
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && (diplomaticTermDefinition is DiplomaticTermTechnologyExchangeDefinition || diplomaticTermDefinition is DiplomaticTermBoosterExchangeDefinition || diplomaticTermDefinition is DiplomaticTermResourceExchangeDefinition))
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, empire, ref list);
					}
				}
			}
		}
		Predicate<GameEntityGUID> <>9__4;
		foreach (DiplomaticTerm diplomaticTerm2 in list)
		{
			if (diplomaticTerm2 is DiplomaticTermBoosterExchange && this.ResourceManager != null)
			{
				DiplomaticTermBoosterExchange BoosterExchange = diplomaticTerm2 as DiplomaticTermBoosterExchange;
				bool flag2 = false;
				foreach (DiplomaticTerm diplomaticTerm3 in diplomaticTerms)
				{
					DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm3 as DiplomaticTermBoosterExchange;
					if (diplomaticTermBoosterExchange != null && diplomaticTermBoosterExchange.Equals(BoosterExchange))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					float num5 = Mathf.Floor((float)this.departmentOfPlanificationAndDevelopment.CountBoosters((BoosterDefinition match) => match.Name == BoosterExchange.BoosterDefinitionName) * this.maximumExchangeAmountRatio);
					float num6 = 0f;
					if (list2.Count > 0)
					{
						num6 = layer.AnalyseContractProposition(list2, this.empire);
					}
					DepartmentOfEducation agency = this.empire.GetAgency<DepartmentOfEducation>();
					if (num5 - 1f > 0f && agency != null)
					{
						int num7 = 1;
						Func<VaultItem, bool> <>9__2;
						while ((float)num7 <= (float)((int)num5))
						{
							DepartmentOfEducation departmentOfEducation = agency;
							Func<VaultItem, bool> predicate;
							if ((predicate = <>9__2) == null)
							{
								predicate = (<>9__2 = ((VaultItem match) => match.Constructible.Name == BoosterExchange.BoosterDefinitionName));
							}
							List<GameEntityGUID> list3 = (from selectedBooster in departmentOfEducation.Where(predicate)
							select selectedBooster.GUID).ToList<GameEntityGUID>();
							List<GameEntityGUID> list4 = list3;
							Predicate<GameEntityGUID> match2;
							if ((match2 = <>9__4) == null)
							{
								match2 = (<>9__4 = ((GameEntityGUID x) => this.ResourceManager.BoostersInUse.Contains(x.ToString())));
							}
							list4.RemoveAll(match2);
							num5 = (float)list3.Count;
							int num8 = num7;
							if (num8 < list3.Count)
							{
								list3.RemoveRange(num8 - 1, list3.Count - num8);
								list3.TrimExcess();
							}
							GameEntityGUID[] boosterGUIDs = list3.ToArray();
							DiplomaticTermBoosterExchange diplomaticTermBoosterExchange2 = new DiplomaticTermBoosterExchange((DiplomaticTermBoosterExchangeDefinition)BoosterExchange.Definition, BoosterExchange.EmpireWhichProposes, BoosterExchange.EmpireWhichProvides, BoosterExchange.EmpireWhichReceives, boosterGUIDs, BoosterExchange.BoosterDefinitionName);
							if (!diplomaticTermBoosterExchange2.CanApply(diplomaticContract, new string[0]))
							{
								break;
							}
							if (layer.AnalyseContractProposition(new List<DiplomaticTerm>
							{
								diplomaticTermBoosterExchange2
							}, this.empire) + num6 + num4 > 0.1f)
							{
								list2.Add(diplomaticTermBoosterExchange2);
								break;
							}
							if ((float)num7 == num5)
							{
								list2.Add(diplomaticTermBoosterExchange2);
							}
							num7++;
						}
						if (list2.Count > 0 && layer.AnalyseContractProposition(list2, this.empire) + num4 > 0.1f)
						{
							if (num3 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num3 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
							{
								flag = true;
								break;
							}
							list2.Clear();
							list2.TrimExcess();
						}
					}
				}
			}
		}
		if (!flag)
		{
			foreach (DiplomaticTerm diplomaticTerm4 in list)
			{
				if (diplomaticTerm4 is DiplomaticTermResourceExchange)
				{
					DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm4 as DiplomaticTermResourceExchange;
					bool flag3 = false;
					foreach (DiplomaticTerm diplomaticTerm5 in diplomaticTerms)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = diplomaticTerm5 as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange2 != null && diplomaticTermResourceExchange2.Equals(diplomaticTermResourceExchange))
						{
							flag3 = true;
							break;
						}
					}
					if (!flag3)
					{
						float num9;
						float num10;
						this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange, out num9, out num10);
						num9 = Mathf.Floor(num9);
						num10 = Mathf.Floor(num10);
						float num11 = 0f;
						if (list2.Count > 0)
						{
							num11 = layer.AnalyseContractProposition(list2, this.empire);
						}
						if (num10 - num9 > 1f)
						{
							float num12 = Mathf.Max(1f, Mathf.Round(num10 / 10f));
							DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = new DiplomaticTermResourceExchange((DiplomaticTermResourceExchangeDefinition)diplomaticTermResourceExchange.Definition, diplomaticTermResourceExchange.EmpireWhichProposes, diplomaticTermResourceExchange.EmpireWhichProvides, diplomaticTermResourceExchange.EmpireWhichReceives, diplomaticTermResourceExchange.ResourceName, 1f);
							float num13 = Mathf.Max(num9, 1f);
							while (num13 <= num10)
							{
								diplomaticTermResourceExchange3.Amount = num13;
								if (!diplomaticTermResourceExchange3.CanApply(diplomaticContract, new string[0]))
								{
									break;
								}
								if (layer.AnalyseContractProposition(new List<DiplomaticTerm>
								{
									diplomaticTermResourceExchange3
								}, this.empire) + num11 + num4 > 0.1f)
								{
									list2.Add(diplomaticTermResourceExchange3);
									break;
								}
								num13 += 1f * num12;
								if (num13 > num10)
								{
									list2.Add(diplomaticTermResourceExchange3);
								}
							}
							if (list2.Count > 0 && layer.AnalyseContractProposition(list2, this.empire) + num4 > 0.1f)
							{
								if (num3 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num3 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
								{
									flag = true;
									break;
								}
								list2.Clear();
								list2.TrimExcess();
							}
						}
					}
				}
			}
		}
		if (!flag)
		{
			foreach (DiplomaticTerm diplomaticTerm6 in list)
			{
				DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = diplomaticTerm6 as DiplomaticTermTechnologyExchange;
				if (diplomaticTermTechnologyExchange != null && diplomaticTermTechnologyExchange.CanApply(diplomaticContract, new string[0]))
				{
					bool flag4 = false;
					foreach (DiplomaticTerm diplomaticTerm7 in diplomaticTerms)
					{
						DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange2 = diplomaticTerm7 as DiplomaticTermTechnologyExchange;
						if (diplomaticTermTechnologyExchange2 != null && diplomaticTermTechnologyExchange2.Equals(diplomaticTermTechnologyExchange))
						{
							flag4 = true;
							break;
						}
					}
					if (!flag4 && DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition) <= 5 && layer.EvaluateDecision(diplomaticTermTechnologyExchange, this.empire, null).Score > 0.1f)
					{
						list2.Add(diplomaticTermTechnologyExchange);
						if (layer.AnalyseContractProposition(list2, this.empire) + num4 > 0.1f)
						{
							if (num3 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num3 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
							{
								flag = true;
								break;
							}
							list2.Clear();
							list2.TrimExcess();
						}
					}
				}
			}
		}
		if (flag)
		{
			diplomaticTerms.AddRange(list2);
			foreach (DiplomaticTerm diplomaticTerm8 in diplomaticTerms)
			{
				if (diplomaticTerm8 is DiplomaticTermBoosterExchange)
				{
					foreach (GameEntityGUID gameEntityGUID in (diplomaticTerm8 as DiplomaticTermBoosterExchange).BoosterGUID)
					{
						this.ResourceManager.BoostersInUse.Add(gameEntityGUID.ToString());
					}
				}
			}
			Diagnostics.Log("ELCP: Empire {0}/{1} with Empire {2}/{3} SweetenDeal succesfull", new object[]
			{
				base.AIEntity.Empire.Index,
				this.AnalyseContractProposition(diplomaticTerms, empire),
				empire.ToString(),
				layer.AnalyseContractProposition(diplomaticTerms, this.empire)
			});
		}
	}

	private void OnVictoryConditionAlertEventRaise(Amplitude.Unity.Event.Event raisedEvent)
	{
		EventVictoryConditionAlert eventVictoryConditionAlert = raisedEvent as EventVictoryConditionAlert;
		if (eventVictoryConditionAlert == null)
		{
			return;
		}
		if (eventVictoryConditionAlert.Empire.Index == base.AIEntity.Empire.Index)
		{
			return;
		}
		if (this.GameDifficulty < 1)
		{
			return;
		}
		global::Empire empire = eventVictoryConditionAlert.Empire;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			bool flag = aiplayer_MajorEmpire.AIState == AIPlayer.PlayerState.EmpireControlledByHuman;
			if (this.departmentOfForeignAffairs.DiplomaticRelations[empire.Index] != null && this.departmentOfForeignAffairs.DiplomaticRelations[empire.Index].State.Name != DiplomaticRelationState.Names.Unknown && this.departmentOfForeignAffairs.DiplomaticRelations[empire.Index].State.Name != DiplomaticRelationState.Names.Dead)
			{
				if ((this.SharedVictory || (this.GameDifficulty < 4 && flag)) && this.departmentOfForeignAffairs.DiplomaticRelations[empire.Index].State.Name == DiplomaticRelationState.Names.Alliance)
				{
					return;
				}
				if (this.GameDifficulty < 3 && flag && this.departmentOfForeignAffairs.DiplomaticRelations[empire.Index].State.Name == DiplomaticRelationState.Names.Peace)
				{
					return;
				}
				this.NeedsVictoryReaction[empire.Index] = true;
				this.AnyVictoryreactionNeeded = true;
			}
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(3);
		base.WriteXml(writer);
		if (num >= 3)
		{
			writer.WriteStartElement("NeedsVictoryReaction");
			for (int i = 0; i < this.NeedsVictoryReaction.Length; i++)
			{
				writer.WriteElementString("Empire_" + i.ToString(), this.NeedsVictoryReaction[i].ToString());
			}
			writer.WriteEndElement();
		}
	}

	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		if (num >= 3)
		{
			reader.ReadStartElement("NeedsVictoryReaction");
			int num2 = 0;
			List<bool> list = new List<bool>();
			while (reader.Reader.Name.Substring(0, 7) == "Empire_")
			{
				bool flag;
				Diagnostics.Assert(bool.TryParse(reader.ReadElementString("Empire_" + num2.ToString()), out flag));
				list.Add(flag);
				if (flag)
				{
					this.AnyVictoryreactionNeeded = true;
				}
				num2++;
			}
			this.NeedsVictoryReaction = list.ToArray();
			reader.ReadEndElement("NeedsVictoryReaction");
		}
	}

	private void GenerateResourceRequest(MajorEmpire opponentEmpire)
	{
		AILayer_Diplomacy ailayer_Diplomacy = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(opponentEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				ailayer_Diplomacy = entity.GetLayer<AILayer_Diplomacy>();
			}
		}
		if (ailayer_Diplomacy == null)
		{
			return;
		}
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && diplomaticTermDefinition is DiplomaticTermResourceExchangeDefinition)
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, opponentEmpire, this.empire, ref list);
					}
				}
			}
		}
		float num = 0f;
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = null;
		for (int j = 0; j < list.Count; j++)
		{
			float num2 = 1f;
			bool flag = false;
			DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = list[j] as DiplomaticTermResourceExchange;
			if (diplomaticTermResourceExchange2.ResourceName == DepartmentOfTheTreasury.Resources.EmpireMoney)
			{
				num2 = 50f;
				flag = true;
			}
			float num3;
			float num4;
			this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange2, out num3, out num4);
			num3 = 5f * num2;
			num4 = Mathf.Floor(num4);
			if (num4 - num3 > 0f)
			{
				float num5 = 0f;
				if (!this.departmentOfTheTreasury.TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, diplomaticTermResourceExchange2.ResourceName, out num5, true))
				{
					num5 = float.MaxValue;
				}
				if (num5 <= 50f * num2)
				{
					float num6 = (50f * num2 - num5) / (25f * num2);
					if (num6 > 1f)
					{
						num6 *= 2f;
					}
					if (flag)
					{
						num6 *= 2f;
					}
					DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = new DiplomaticTermResourceExchange((DiplomaticTermResourceExchangeDefinition)diplomaticTermResourceExchange2.Definition, diplomaticTermResourceExchange2.EmpireWhichProposes, diplomaticTermResourceExchange2.EmpireWhichProvides, diplomaticTermResourceExchange2.EmpireWhichReceives, diplomaticTermResourceExchange2.ResourceName, 0f);
					float num7 = Mathf.Max(1f, Mathf.Round(num4 / 20f));
					float num8 = num4;
					while (num8 >= num3)
					{
						diplomaticTermResourceExchange3.Amount = num8;
						if (!diplomaticTermResourceExchange3.CanApply(diplomaticContract, new string[0]))
						{
							break;
						}
						float num9 = this.AnalyseContractProposition(new List<DiplomaticTerm>
						{
							diplomaticTermResourceExchange3
						}, opponentEmpire);
						if (num9 <= ((!this.DiplomacyFocus) ? 2.1f : 1.1f))
						{
							break;
						}
						num9 *= num6;
						float num10 = ailayer_Diplomacy.AnalyseContractProposition(new List<DiplomaticTerm>
						{
							diplomaticTermResourceExchange3
						}, this.empire);
						if (Mathf.Abs(num10) > num9 * 3f)
						{
							break;
						}
						if (num10 < -8f)
						{
							num8 -= 1f * num7;
						}
						else
						{
							if (num9 <= num)
							{
								break;
							}
							num = num9;
							diplomaticTermResourceExchange = new DiplomaticTermResourceExchange((DiplomaticTermResourceExchangeDefinition)diplomaticTermResourceExchange2.Definition, diplomaticTermResourceExchange2.EmpireWhichProposes, diplomaticTermResourceExchange2.EmpireWhichProvides, diplomaticTermResourceExchange2.EmpireWhichReceives, diplomaticTermResourceExchange2.ResourceName, num8);
							if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
							{
								Diagnostics.Log("ELCP {3} with {4}: {0} of {1} is current best term with {2}", new object[]
								{
									diplomaticTermResourceExchange3.Amount,
									diplomaticTermResourceExchange2.ResourceName,
									num9,
									base.AIEntity.Empire,
									opponentEmpire
								});
								break;
							}
							break;
						}
					}
				}
			}
		}
		if (diplomaticTermResourceExchange != null)
		{
			float num11 = this.DiplomacyFocus ? 3f : 8f;
			float num12;
			float num13;
			if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num12, out num13))
			{
				AILayer.LogError("Can't retrieve empire point account infos");
			}
			float num14 = 0f;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num14, false);
			float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTermResourceExchange, this.empire);
			if (empirePointCost <= num12 || empirePointCost * num11 < num14)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
				contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				contractRequest.Terms.Add(diplomaticTermResourceExchange);
				this.ContractRequests.Add(contractRequest);
				return;
			}
		}
	}

	private void GenerateTechRequest(MajorEmpire opponentEmpire)
	{
		if (this.departmentOfScience.CurrentTechnologyEraNumber > 5 && !this.DiplomacyFocus)
		{
			return;
		}
		AILayer_Diplomacy ailayer_Diplomacy = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(opponentEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				ailayer_Diplomacy = entity.GetLayer<AILayer_Diplomacy>();
			}
		}
		if (ailayer_Diplomacy == null)
		{
			return;
		}
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && diplomaticTermDefinition is DiplomaticTermTechnologyExchangeDefinition)
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, opponentEmpire, this.empire, ref list);
					}
				}
			}
		}
		float num = 0f;
		DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = null;
		for (int j = 0; j < list.Count; j++)
		{
			DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange2 = list[j] as DiplomaticTermTechnologyExchange;
			if (DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange2.TechnologyDefinition) <= 5 && this.departmentOfScience.GetTechnologyState(diplomaticTermTechnologyExchange2.TechnologyDefinition) != DepartmentOfScience.ConstructibleElement.State.Queued && diplomaticTermTechnologyExchange2.CanApply(diplomaticContract, new string[0]))
			{
				float num2 = this.AnalyseContractProposition(new List<DiplomaticTerm>
				{
					diplomaticTermTechnologyExchange2
				}, opponentEmpire);
				if (num2 >= ((!this.DiplomacyFocus) ? 3f : 1.1f) && Mathf.Abs(ailayer_Diplomacy.AnalyseContractProposition(new List<DiplomaticTerm>
				{
					diplomaticTermTechnologyExchange2
				}, this.empire)) <= num2 * ((!this.DiplomacyFocus) ? 3f : 4f) && num2 > num)
				{
					num = num2;
					diplomaticTermTechnologyExchange = diplomaticTermTechnologyExchange2;
				}
			}
		}
		if (diplomaticTermTechnologyExchange != null)
		{
			float num3 = this.DiplomacyFocus ? 3f : 5f;
			float num4;
			float num5;
			if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num4, out num5))
			{
				AILayer.LogError("Can't retrieve empire point account infos");
			}
			float num6 = 0f;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num6, false);
			float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTermTechnologyExchange, this.empire);
			if (empirePointCost <= num4 || empirePointCost * num3 < num6)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
				contractRequest.Terms.Add(diplomaticTermTechnologyExchange);
				contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				this.ContractRequests.Add(contractRequest);
				return;
			}
		}
	}

	private void RelationStateChangeToTruce(global::Empire opponent)
	{
		if (this.GameDifficulty < 4)
		{
			this.NeedsVictoryReaction[opponent.Index] = false;
		}
	}

	private bool TryVictoryAlertDeclaration(MajorEmpire opponentEmpire)
	{
		bool flag = false;
		float num = opponentEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
		if (this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > 4f * num)
		{
			flag = true;
		}
		else if (this.MilitaryPowerDif > 0.4f * num)
		{
			flag = true;
		}
		else if (this.MilitaryPowerDif > 0.1f * num)
		{
			DepartmentOfForeignAffairs agency = opponentEmpire.GetAgency<DepartmentOfForeignAffairs>();
			for (int i = 0; i < this.majorEmpires.Length; i++)
			{
				MajorEmpire majorEmpire = this.majorEmpires[i];
				Diagnostics.Assert(majorEmpire != null);
				if (majorEmpire.Index != this.empire.Index && majorEmpire.Index != opponentEmpire.Index && agency.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.War)
				{
					num -= majorEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
				}
			}
			if (this.MilitaryPowerDif > 0.5f * num)
			{
				flag = true;
			}
		}
		if (flag)
		{
			this.AlwaysProcess = true;
			this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.War);
			this.AlwaysProcess = false;
		}
		return flag;
	}

	private bool TryVictoryAlertGangUp(MajorEmpire AlliedEmpire)
	{
		StaticString askToDeclareWar = AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar;
		DepartmentOfForeignAffairs agency = AlliedEmpire.GetAgency<DepartmentOfForeignAffairs>();
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			Diagnostics.Assert(majorEmpire != null);
			DiplomaticTermProposal item;
			DiplomaticTermProposal item2;
			if (majorEmpire.Index != this.empire.Index && majorEmpire.Index != AlliedEmpire.Index && Array.IndexOf<int>(AILayer_Diplomacy.VictoryTargets, majorEmpire.Index) < 0 && this.NeedsVictoryReaction[majorEmpire.Index] && this.departmentOfForeignAffairs.DiplomaticRelations[majorEmpire.Index].State.Name != DiplomaticRelationState.Names.War && agency.DiplomaticRelations[majorEmpire.Index].State.Name != DiplomaticRelationState.Names.War && agency.DiplomaticRelations[majorEmpire.Index].State.Name != DiplomaticRelationState.Names.Unknown && agency.DiplomaticRelations[majorEmpire.Index].State.Name != DiplomaticRelationState.Names.Dead && this.MilitaryPowerDif + AlliedEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower) > 0.5f * majorEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower) && ((!this.SharedVictory && this.GameDifficulty >= 3) || !(agency.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.Alliance)) && (this.GameDifficulty >= 2 || !(agency.DiplomaticRelations[majorEmpire.Index].State.Name == DiplomaticRelationState.Names.Peace)) && this.TryGenerateAskToDiplomaticTerm(AlliedEmpire, majorEmpire, askToDeclareWar, out item) && this.TryGenerateReversedAskToDiplomaticTerm(AlliedEmpire, majorEmpire, askToDeclareWar, out item2))
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, AlliedEmpire);
				contractRequest.Terms.Add(item);
				contractRequest.Terms.Add(item2);
				contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				this.ContractRequests.Add(contractRequest);
				AILayer_Diplomacy.VictoryTargets[this.empire.Index] = majorEmpire.Index;
				AILayer_Diplomacy.VictoryTargets[AlliedEmpire.Index] = majorEmpire.Index;
				return true;
			}
		}
		return false;
	}

	private bool TryGenerateReversedAskToDiplomaticTerm(global::Empire alliedEmpire, global::Empire commonEnemy, StaticString proposalTermName, out DiplomaticTermProposal proposal)
	{
		proposal = null;
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, alliedEmpire);
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && diplomaticTermDefinition is DiplomaticTermProposalDefinition && diplomaticTermDefinition.Name == "AskToDeclareWar")
					{
						proposal = new DiplomaticTermProposal(diplomaticTermDefinition as DiplomaticTermProposalDefinition, this.empire, this.empire, alliedEmpire);
						break;
					}
				}
			}
		}
		if (proposal == null)
		{
			return false;
		}
		proposal.ChangeEmpire(diplomaticContract, commonEnemy);
		if (!proposal.CanApply(diplomaticContract, new string[0]))
		{
			return false;
		}
		float num;
		float num2;
		if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num, out num2))
		{
			AILayer.LogError("Can't retrieve empire point account infos");
		}
		float num3 = 0f;
		base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num3, false);
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(proposal, this.empire);
		return empirePointCost <= num || empirePointCost * 3f < num3;
	}

	public List<Agent> GetAgents(MajorEmpire opponentEmpire)
	{
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		AgentGroup agentGroupForEmpire = this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire);
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		List<Agent> list = new List<Agent>();
		Diagnostics.Assert(this.diplomaticRelationStateByAmasAgentName != null);
		foreach (KeyValuePair<StaticString, DiplomaticTermDefinition[]> keyValuePair in this.diplomaticTermByAmasAgentName)
		{
			Agent agent = agentGroupForEmpire.GetAgent(keyValuePair.Key);
			Diagnostics.Assert(agent != null);
			bool flag = false;
			foreach (DiplomaticTermDefinition constructibleElement in keyValuePair.Value)
			{
				if (DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, this.empire, opponentEmpire, constructibleElement, new string[0]))
				{
					flag = true;
					break;
				}
			}
			if (flag)
			{
				list.Add(agent);
			}
		}
		List<Agent> list2 = new List<Agent>();
		foreach (KeyValuePair<StaticString, DiplomaticRelationState> keyValuePair2 in this.diplomaticRelationStateByAmasAgentName)
		{
			Agent agent2 = agentGroupForEmpire.GetAgent(keyValuePair2.Key);
			Diagnostics.Assert(agent2 != null && agent2.CriticityMax != null);
			Diagnostics.Assert(keyValuePair2.Value != null);
			StaticString name = keyValuePair2.Value.Name;
			if (agent2.Enable)
			{
				list2.Add(agent2);
			}
		}
		list.Sort((Agent left, Agent right) => -1 * left.CriticityMax.Intensity.CompareTo(right.CriticityMax.Intensity));
		list2.Sort((Agent left, Agent right) => -1 * left.CriticityMax.Intensity.CompareTo(right.CriticityMax.Intensity));
		list2.AddRange(list);
		list.Clear();
		foreach (KeyValuePair<StaticString, DiplomaticTermDefinition[]> keyValuePair3 in this.proposalTermByAmasAgentName)
		{
			Agent agent3 = agentGroupForEmpire.GetAgent(keyValuePair3.Key);
			Diagnostics.Assert(agent3 != null);
			bool flag2 = false;
			foreach (DiplomaticTermDefinition constructibleElement2 in keyValuePair3.Value)
			{
				if (DepartmentOfForeignAffairs.CheckConstructiblePrerequisites(diplomaticContract, opponentEmpire, this.empire, constructibleElement2, new string[0]))
				{
					flag2 = true;
					break;
				}
			}
			if (flag2)
			{
				list.Add(agent3);
			}
		}
		list.Sort((Agent left, Agent right) => -1 * left.CriticityMax.Intensity.CompareTo(right.CriticityMax.Intensity));
		list2.AddRange(list);
		return list2;
	}

	private void GeneratePrisonerDeal(MajorEmpire opponentEmpire)
	{
		if (this.departmentOfEducation.MyCapturedHeroes.Count == 0 && this.departmentOfEducation.Prisoners.Count == 0)
		{
			return;
		}
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && diplomaticTermDefinition is DiplomaticTermPrisonerExchangeDefinition)
					{
						if (this.departmentOfEducation.Prisoners.Count > 0)
						{
							DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, opponentEmpire, ref list);
						}
						if (this.departmentOfEducation.MyCapturedHeroes.Count > 0)
						{
							DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, opponentEmpire, this.empire, ref list);
						}
					}
				}
			}
		}
		for (int j = 0; j < list.Count; j++)
		{
			DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange = list[j] as DiplomaticTermPrisonerExchange;
			if (diplomaticTermPrisonerExchange.CanApply(diplomaticContract, new string[0]))
			{
				float num = this.AnalyseContractProposition(new List<DiplomaticTerm>
				{
					diplomaticTermPrisonerExchange
				}, opponentEmpire);
				if ((diplomaticTermPrisonerExchange.EmpireWhichProvides.Index == opponentEmpire.Index && num > 2f) || diplomaticTermPrisonerExchange.EmpireWhichProvides.Index == this.empire.Index)
				{
					if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
					{
						Diagnostics.Log("{0} GeneratePrisonerDeal", new object[]
						{
							diplomaticTermPrisonerExchange.ToString()
						});
					}
					float num2;
					float num3;
					if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num2, out num3))
					{
						AILayer.LogError("Can't retrieve empire point account infos");
					}
					if (DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTermPrisonerExchange, this.empire) <= num2)
					{
						AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
						contractRequest.Terms.Add(diplomaticTermPrisonerExchange);
						contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
						this.ContractRequests.Add(contractRequest);
						return;
					}
				}
			}
		}
	}

	public bool GetPeaceWish(int index)
	{
		return index >= 0 && index < this.majorEmpires.Length && this.PeaceWish[index] && this.departmentOfForeignAffairs.DiplomaticRelations[index].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[index].State.Name != DiplomaticRelationState.Names.War;
	}

	private void DecideIfKeepProposal(DiplomaticTermProposal diplomaticTermProposal, DiplomaticContract contract, DepartmentOfForeignAffairs partnerForeign, global::Empire ThirdParty, ref bool removeMyProposals, ref bool removeOthersProposal)
	{
		global::Empire empire = (diplomaticTermProposal.EmpireWhichProvides != this.empire) ? diplomaticTermProposal.EmpireWhichProvides : diplomaticTermProposal.EmpireWhichReceives;
		List<global::Empire> list = new List<global::Empire>();
		if (diplomaticTermProposal.TryGetValidEmpires(contract, ref list))
		{
			list.RemoveAll((global::Empire x) => diplomaticTermProposal.EmpireWhichProvides.GetPropertyValue(SimulationProperties.MilitaryPower) * 5f < x.GetPropertyValue(SimulationProperties.MilitaryPower));
			if (diplomaticTermProposal.EmpireWhichProvides == this.empire)
			{
				list.RemoveAll((global::Empire x) => this.departmentOfForeignAffairs.IsFriend(x));
			}
			if (diplomaticTermProposal.EmpireWhichProvides == empire)
			{
				list.RemoveAll((global::Empire x) => partnerForeign.IsFriend(x));
			}
			if (list.Count > 0)
			{
				global::Empire empire2 = null;
				if (ThirdParty != null && list.Contains(ThirdParty))
				{
					empire2 = ThirdParty;
					removeOthersProposal = true;
				}
				else
				{
					bool flag = false;
					if (diplomaticTermProposal.EmpireWhichProvides == empire)
					{
						removeOthersProposal = true;
						if (list.FindAll((global::Empire x) => this.departmentOfForeignAffairs.IsAtWarWith(x)).Count > 0)
						{
							flag = true;
						}
					}
					else if (diplomaticTermProposal.EmpireWhichProvides == this.empire)
					{
						removeMyProposals = true;
						if (list.FindAll((global::Empire x) => partnerForeign.IsAtWarWith(x)).Count > 0)
						{
							flag = true;
						}
					}
					float num = 0f;
					for (int i = 0; i < list.Count; i++)
					{
						if (!flag || diplomaticTermProposal.EmpireWhichReceives.GetAgency<DepartmentOfForeignAffairs>().IsAtWarWith(list[i]))
						{
							diplomaticTermProposal.ChangeEmpire(contract, list[i]);
							DecisionResult decisionResult = this.EvaluateDecision(diplomaticTermProposal, empire, null);
							if (decisionResult.Score > num)
							{
								num = decisionResult.Score;
								empire2 = list[i];
							}
						}
					}
				}
				if (empire2 != null)
				{
					diplomaticTermProposal.ChangeEmpire(contract, empire2);
				}
			}
		}
	}

	private bool ModifyOrRemoveBoosterExchange(global::Empire OtherEmpire, AILayer_ResourceManager OthersResourceManager, ref DiplomaticTermBoosterExchange diplomaticTermbooster)
	{
		AILayer_ResourceManager ailayer_ResourceManager = (diplomaticTermbooster.EmpireWhichProvides == this.empire) ? this.ResourceManager : OthersResourceManager;
		if (diplomaticTermbooster.BoosterGUID.Length != 0)
		{
			if (!ailayer_ResourceManager.BoostersInUse.Contains(diplomaticTermbooster.BoosterGUID[0].ToString()))
			{
				return false;
			}
			diplomaticTermbooster.BoosterGUID[0] = GameEntityGUID.Zero;
			DepartmentOfEducation departmentOfEducation = (diplomaticTermbooster.EmpireWhichProvides == this.empire) ? this.departmentOfEducation : OtherEmpire.GetAgency<DepartmentOfEducation>();
			string Name = diplomaticTermbooster.BoosterDefinitionName.ToString();
			Func<VaultItem, bool> predicate;
			Func<VaultItem, bool> <>9__0;
			if ((predicate = <>9__0) == null)
			{
				predicate = (<>9__0 = ((VaultItem match) => match.Constructible.Name == Name));
			}
			foreach (GameEntityGUID gameEntityGUID in (from selectedBooster in departmentOfEducation.Where(predicate)
			select selectedBooster.GUID).ToList<GameEntityGUID>())
			{
				if (!ailayer_ResourceManager.BoostersInUse.Contains(gameEntityGUID.ToString()))
				{
					diplomaticTermbooster.BoosterGUID[0] = gameEntityGUID;
					break;
				}
			}
			if (diplomaticTermbooster.BoosterGUID[0].IsValid)
			{
				return false;
			}
		}
		return true;
	}

	private void GenerateSymbiosisRequest(MajorEmpire opponentEmpire)
	{
		if (this.departmentOfTheInterior.MainCity == null || !this.departmentOfTheInterior.MainCity.WorldPosition.IsValid || this.departmentOfTheInterior.MainCity.Empire.Index != this.empire.Index)
		{
			return;
		}
		DepartmentOfTheInterior agency = opponentEmpire.GetAgency<DepartmentOfTheInterior>();
		if (agency.NonInfectedCities.Count < 4)
		{
			return;
		}
		AILayer_Diplomacy ailayer_Diplomacy = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(opponentEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				ailayer_Diplomacy = entity.GetLayer<AILayer_Diplomacy>();
			}
		}
		if (ailayer_Diplomacy == null)
		{
			return;
		}
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && diplomaticTermDefinition is DiplomaticTermCityExchangeDefinition && diplomaticTermDefinition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, opponentEmpire, this.empire, ref list);
					}
				}
			}
		}
		if (list.Count == 0)
		{
			return;
		}
		List<City> cities = new List<City>();
		foreach (DiplomaticTerm diplomaticTerm in list)
		{
			DiplomaticTermCityExchange cityex = diplomaticTerm as DiplomaticTermCityExchange;
			City city = agency.NonInfectedCities.FirstOrDefault((City c) => c.GUID == cityex.CityGUID);
			if (city != null)
			{
				cities.Add(city);
			}
		}
		cities.Sort((City left, City right) => this.worldPositionningService.GetDistance(left.WorldPosition, this.departmentOfTheInterior.MainCity.WorldPosition).CompareTo(this.worldPositionningService.GetDistance(right.WorldPosition, this.departmentOfTheInterior.MainCity.WorldPosition)));
		DiplomaticTermCityExchange diplomaticTermCityExchange = null;
		int j = 0;
		float num = 0f;
		Func<DiplomaticTerm, bool> <>9__2;
		while (j < cities.Count)
		{
			IEnumerable<DiplomaticTerm> source = list;
			Func<DiplomaticTerm, bool> predicate;
			if ((predicate = <>9__2) == null)
			{
				predicate = (<>9__2 = ((DiplomaticTerm t) => t is DiplomaticTermCityExchange && (t as DiplomaticTermCityExchange).CityGUID == cities[j].GUID));
			}
			DiplomaticTermCityExchange diplomaticTermCityExchange2 = source.FirstOrDefault(predicate) as DiplomaticTermCityExchange;
			if (diplomaticTermCityExchange2 != null && diplomaticTermCityExchange2.CanApply(diplomaticContract, new string[0]))
			{
				float num2 = this.AnalyseContractProposition(new List<DiplomaticTerm>
				{
					diplomaticTermCityExchange2
				}, opponentEmpire);
				float num3 = ailayer_Diplomacy.AnalyseContractProposition(new List<DiplomaticTerm>
				{
					diplomaticTermCityExchange2
				}, this.empire);
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {1} GenerateSymbiosisRequest city {0} scores {2} + {3} {4} = {5}", new object[]
					{
						cities[j].LocalizedName,
						this.empire,
						num2,
						this.SweetenDealThreshold,
						num3,
						num2 + this.SweetenDealThreshold + num3
					});
				}
				num2 += this.SweetenDealThreshold + num3;
				if (num2 > 0f && num2 > num)
				{
					num = num2;
					diplomaticTermCityExchange = diplomaticTermCityExchange2;
				}
				int k = j;
				j = k + 1;
			}
		}
		if (diplomaticTermCityExchange != null)
		{
			float num4 = this.DiplomacyFocus ? 3f : 4f;
			float num5;
			float num6;
			if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num5, out num6))
			{
				AILayer.LogError("Can't retrieve empire point account infos");
			}
			float num7 = 0f;
			base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>().TryGetResourceStockValue(base.AIEntity.Empire.SimulationObject, DepartmentOfTheTreasury.Resources.EmpirePoint, out num7, false);
			float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTermCityExchange, this.empire);
			if (empirePointCost <= num5 || empirePointCost * num4 < num7)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
				contractRequest.Terms.Add(diplomaticTermCityExchange);
				contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
				this.ContractRequests.Add(contractRequest);
				return;
			}
		}
	}

	public float GetMilitaryPowerDif(bool forceUpdate = false)
	{
		if (this.lastUpdate != this.game.Turn || forceUpdate)
		{
			float propertyValue = this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
			this.MilitaryPowerDif = propertyValue;
			this.resourceWarReserveFactor = 1f;
			if (this.departmentOfForeignAffairs.IsInWarWithSomeone())
			{
				foreach (MajorEmpire majorEmpire in Array.FindAll<MajorEmpire>(this.majorEmpires, (MajorEmpire x) => x.Index != this.empire.Index && this.departmentOfForeignAffairs.DiplomaticRelations[x.Index].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[x.Index].State.Name == DiplomaticRelationState.Names.War))
				{
					this.MilitaryPowerDif -= majorEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
				}
				if (this.MilitaryPowerDif >= 0f)
				{
					this.resourceWarReserveFactor = 2f - this.MilitaryPowerDif / this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
				}
				else
				{
					this.resourceWarReserveFactor = 2f - 4f * this.MilitaryPowerDif / this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
				}
			}
			this.lastUpdate = this.game.Turn;
		}
		return this.MilitaryPowerDif;
	}

	public DiplomaticContract SetCurrentAnswerContract
	{
		set
		{
			this.CurrentAnswerContract = value;
			if (ELCPUtilities.UseELCPMultiThreading)
			{
				if (this.CurrentAnswerContract != null)
				{
					Monitor.Enter(this.CurrentAnswerContract);
					return;
				}
				Monitor.Exit(this.CurrentAnswerContract);
			}
		}
	}

	private void OrderFillContractELCP(AILayer_Diplomacy.ContractRequest contractRequest, bool Multi)
	{
		this.CurrentAnswerContract = contractRequest.Contract;
		Diagnostics.Assert(contractRequest.Contract != null);
		global::Empire empire = (contractRequest.Contract.EmpireWhichProposes.Index != this.empire.Index) ? contractRequest.Contract.EmpireWhichProposes : contractRequest.Contract.EmpireWhichReceives;
		if (Multi)
		{
			Monitor.Enter(this.CurrentAnswerContract);
			Diagnostics.Log("ELCP: Empire {0} with {2} OrderFillContract QueueUserWorkItem Thread {1}", new object[]
			{
				base.AIEntity.Empire.Index,
				Thread.CurrentThread.ManagedThreadId,
				empire
			});
			Monitor.Enter(this.empire.SimulationObject);
			Monitor.Enter(empire.SimulationObject);
		}
		this.FillContract(contractRequest.Contract, ref contractRequest.Terms);
		this.AnalyseContractProposition(contractRequest.Terms, empire);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (contractRequest.Contract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Negotiation && this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				AILayer_Diplomacy layer = entity.GetLayer<AILayer_Diplomacy>();
				if (layer != null && layer.IsActive() && layer.AnalyseContractProposition(contractRequest.Terms, this.empire) < 0.1f)
				{
					this.SweetenDeal(contractRequest.Contract, ref contractRequest.Terms, empire, layer);
				}
			}
		}
		if (Multi)
		{
			Monitor.Exit(this.CurrentAnswerContract);
			Monitor.Exit(this.empire.SimulationObject);
			Monitor.Exit(empire.SimulationObject);
		}
		this.CurrentAnswerContract = null;
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.FillContract;
		int count = contractRequest.Contract.Terms.Count;
		DiplomaticTermChange[] array = new DiplomaticTermChange[count + contractRequest.Terms.Count];
		for (int i = count - 1; i >= 0; i--)
		{
			DiplomaticTerm diplomaticTerm = contractRequest.Contract.Terms[i];
			array[i] = DiplomaticTermChange.Remove(diplomaticTerm.Index);
		}
		for (int j = 0; j < contractRequest.Terms.Count; j++)
		{
			DiplomaticTerm diplomaticTerm2 = contractRequest.Terms[j];
			array[count + j] = DiplomaticTermChange.Add(diplomaticTerm2);
			Diagnostics.Log("ELCP: Empire {0} OrderFillContract adding term {1}", new object[]
			{
				base.AIEntity.Empire.Index,
				diplomaticTerm2.ToString()
			});
		}
		OrderChangeDiplomaticContractTermsCollection orderChangeDiplomaticContractTermsCollection = new OrderChangeDiplomaticContractTermsCollection(contractRequest.Contract, array);
		Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractTermsCollection, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
		contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractTermsCollection.TicketNumber;
	}

	private IEnumerator OrderFillContractCoroutine(AILayer_Diplomacy.ContractRequest contractRequest)
	{
		this.CurrentAnswerContract = contractRequest.Contract;
		Diagnostics.Assert(contractRequest.Contract != null);
		global::Empire empire = (contractRequest.Contract.EmpireWhichProposes.Index != this.empire.Index) ? contractRequest.Contract.EmpireWhichProposes : contractRequest.Contract.EmpireWhichReceives;
		this.currentDiplomaticTerms = contractRequest.Terms;
		yield return this.FillContractCoroutine(contractRequest.Contract);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (contractRequest.Contract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Negotiation && this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				AILayer_Diplomacy layer = entity.GetLayer<AILayer_Diplomacy>();
				if (layer != null && layer.IsActive() && layer.AnalyseContractProposition(this.currentDiplomaticTerms, this.empire) < 0.1f)
				{
					yield return this.SweetenDealCoroutine(contractRequest.Contract, empire, layer);
				}
			}
		}
		this.CurrentAnswerContract = null;
		contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.FillContract;
		int count = contractRequest.Contract.Terms.Count;
		DiplomaticTermChange[] array = new DiplomaticTermChange[count + this.currentDiplomaticTerms.Count];
		for (int i = count - 1; i >= 0; i--)
		{
			DiplomaticTerm diplomaticTerm = contractRequest.Contract.Terms[i];
			array[i] = DiplomaticTermChange.Remove(diplomaticTerm.Index);
		}
		for (int j = 0; j < this.currentDiplomaticTerms.Count; j++)
		{
			DiplomaticTerm diplomaticTerm2 = this.currentDiplomaticTerms[j];
			array[count + j] = DiplomaticTermChange.Add(diplomaticTerm2);
			Diagnostics.Log("ELCP: Empire {0} OrderFillContract adding term {1}", new object[]
			{
				base.AIEntity.Empire.Index,
				diplomaticTerm2.ToString()
			});
		}
		OrderChangeDiplomaticContractTermsCollection orderChangeDiplomaticContractTermsCollection = new OrderChangeDiplomaticContractTermsCollection(contractRequest.Contract, array);
		Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractTermsCollection, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
		contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractTermsCollection.TicketNumber;
		this.currentDiplomaticTerms.Clear();
		yield break;
	}

	private IEnumerator SweetenDealCoroutine(DiplomaticContract diplomaticContract, global::Empire empire, AILayer_Diplomacy layer)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		if (this.currentDiplomaticTerms == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		float num = 1f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(empire))
		{
			num = 2f;
		}
		else if (this.MilitaryPowerDif < 0f)
		{
			foreach (DiplomaticTerm diplomaticTerm7 in this.currentDiplomaticTerms)
			{
				DiplomaticRelationStateTermDefinition diplomaticRelationStateTermDefinition = diplomaticTerm7.Definition as DiplomaticRelationStateTermDefinition;
				if (diplomaticRelationStateTermDefinition != null && (diplomaticRelationStateTermDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.Peace || diplomaticRelationStateTermDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.Alliance))
				{
					float num4 = Mathf.Min(1f, Mathf.Abs(this.MilitaryPowerDif) / this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower));
					num += num4;
					break;
				}
			}
		}
		DiplomaticTermProposal diplomaticTermProposal = this.currentDiplomaticTerms.Find((DiplomaticTerm y) => y is DiplomaticTermProposal && y.EmpireWhichProvides.Index == empire.Index) as DiplomaticTermProposal;
		if (diplomaticTermProposal != null && (this.departmentOfForeignAffairs.IsAtWarWith(diplomaticTermProposal.ChosenEmpire) || this.NeedsVictoryReaction[diplomaticTermProposal.ChosenEmpireIndex]))
		{
			num *= 2f;
		}
		if (this.DiplomacyFocus)
		{
			num *= 1.5f;
		}
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		float num2 = this.AnalyseContractProposition(this.currentDiplomaticTerms, empire);
		float num3 = layer.AnalyseContractProposition(this.currentDiplomaticTerms, this.empire);
		List<DiplomaticTerm> list2 = new List<DiplomaticTerm>();
		bool flag = false;
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && (diplomaticTermDefinition is DiplomaticTermTechnologyExchangeDefinition || diplomaticTermDefinition is DiplomaticTermBoosterExchangeDefinition || diplomaticTermDefinition is DiplomaticTermResourceExchangeDefinition))
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, empire, ref list);
					}
				}
			}
		}
		Predicate<GameEntityGUID> <>9__4;
		foreach (DiplomaticTerm diplomaticTerm6 in list)
		{
			if (diplomaticTerm6 is DiplomaticTermBoosterExchange && this.ResourceManager != null)
			{
				AILayer_Diplomacy.<>c__DisplayClass0_1__26 CS$<>8__locals2 = new AILayer_Diplomacy.<>c__DisplayClass0_1__26();
				yield return null;
				CS$<>8__locals2.BoosterExchange = (diplomaticTerm6 as DiplomaticTermBoosterExchange);
				bool flag2 = false;
				foreach (DiplomaticTerm diplomaticTerm8 in this.currentDiplomaticTerms)
				{
					DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm8 as DiplomaticTermBoosterExchange;
					if (diplomaticTermBoosterExchange != null && diplomaticTermBoosterExchange.Equals(CS$<>8__locals2.BoosterExchange))
					{
						flag2 = true;
						break;
					}
				}
				if (!flag2)
				{
					float num5 = Mathf.Floor((float)this.departmentOfPlanificationAndDevelopment.CountBoosters((BoosterDefinition match) => match.Name == CS$<>8__locals2.BoosterExchange.BoosterDefinitionName) * this.maximumExchangeAmountRatio);
					float num6 = 0f;
					if (list2.Count > 0)
					{
						num6 = layer.AnalyseContractProposition(list2, this.empire);
					}
					DepartmentOfEducation agency = this.empire.GetAgency<DepartmentOfEducation>();
					if (num5 - 1f > 0f && agency != null)
					{
						int num7 = 1;
						while ((float)num7 <= (float)((int)num5))
						{
							DepartmentOfEducation departmentOfEducation = agency;
							Func<VaultItem, bool> predicate;
							if ((predicate = CS$<>8__locals2.<>9__2) == null)
							{
								predicate = (CS$<>8__locals2.<>9__2 = ((VaultItem match) => match.Constructible.Name == CS$<>8__locals2.BoosterExchange.BoosterDefinitionName));
							}
							List<GameEntityGUID> list3 = (from selectedBooster in departmentOfEducation.Where(predicate)
							select selectedBooster.GUID).ToList<GameEntityGUID>();
							List<GameEntityGUID> list4 = list3;
							Predicate<GameEntityGUID> match2;
							if ((match2 = <>9__4) == null)
							{
								match2 = (<>9__4 = ((GameEntityGUID x) => this.ResourceManager.BoostersInUse.Contains(x.ToString())));
							}
							list4.RemoveAll(match2);
							num5 = (float)list3.Count;
							int num8 = num7;
							if (num8 < list3.Count)
							{
								list3.RemoveRange(num8 - 1, list3.Count - num8);
								list3.TrimExcess();
							}
							GameEntityGUID[] boosterGUIDs = list3.ToArray();
							DiplomaticTermBoosterExchange diplomaticTermBoosterExchange2 = new DiplomaticTermBoosterExchange((DiplomaticTermBoosterExchangeDefinition)CS$<>8__locals2.BoosterExchange.Definition, CS$<>8__locals2.BoosterExchange.EmpireWhichProposes, CS$<>8__locals2.BoosterExchange.EmpireWhichProvides, CS$<>8__locals2.BoosterExchange.EmpireWhichReceives, boosterGUIDs, CS$<>8__locals2.BoosterExchange.BoosterDefinitionName);
							if (!diplomaticTermBoosterExchange2.CanApply(diplomaticContract, new string[0]))
							{
								break;
							}
							if (layer.AnalyseContractProposition(new List<DiplomaticTerm>
							{
								diplomaticTermBoosterExchange2
							}, this.empire) + num6 + num3 > 0.1f)
							{
								list2.Add(diplomaticTermBoosterExchange2);
								break;
							}
							if ((float)num7 == num5)
							{
								list2.Add(diplomaticTermBoosterExchange2);
							}
							num7++;
						}
						if (list2.Count > 0 && layer.AnalyseContractProposition(list2, this.empire) + num3 > 0.1f)
						{
							if (num2 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num2 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
							{
								flag = true;
								break;
							}
							list2.Clear();
							list2.TrimExcess();
						}
					}
				}
				CS$<>8__locals2 = null;
			}
			diplomaticTerm6 = null;
		}
		List<DiplomaticTerm>.Enumerator enumerator2 = default(List<DiplomaticTerm>.Enumerator);
		if (!flag)
		{
			foreach (DiplomaticTerm diplomaticTerm6 in list)
			{
				if (diplomaticTerm6 is DiplomaticTermResourceExchange)
				{
					yield return null;
					DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm6 as DiplomaticTermResourceExchange;
					bool flag3 = false;
					foreach (DiplomaticTerm diplomaticTerm9 in this.currentDiplomaticTerms)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = diplomaticTerm9 as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange2 != null && diplomaticTermResourceExchange2.Equals(diplomaticTermResourceExchange))
						{
							flag3 = true;
							break;
						}
					}
					if (!flag3)
					{
						float num9;
						float num10;
						this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange, out num9, out num10);
						num9 = Mathf.Floor(num9);
						num10 = Mathf.Floor(num10);
						float num11 = 0f;
						if (list2.Count > 0)
						{
							num11 = layer.AnalyseContractProposition(list2, this.empire);
						}
						if (num10 - num9 > 1f)
						{
							float num12 = Mathf.Max(1f, Mathf.Round(num10 / 10f));
							DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = new DiplomaticTermResourceExchange((DiplomaticTermResourceExchangeDefinition)diplomaticTermResourceExchange.Definition, diplomaticTermResourceExchange.EmpireWhichProposes, diplomaticTermResourceExchange.EmpireWhichProvides, diplomaticTermResourceExchange.EmpireWhichReceives, diplomaticTermResourceExchange.ResourceName, 1f);
							float num13 = Mathf.Max(num9, 1f);
							while (num13 <= num10)
							{
								diplomaticTermResourceExchange3.Amount = num13;
								if (!diplomaticTermResourceExchange3.CanApply(diplomaticContract, new string[0]))
								{
									break;
								}
								if (layer.AnalyseContractProposition(new List<DiplomaticTerm>
								{
									diplomaticTermResourceExchange3
								}, this.empire) + num11 + num3 > 0.1f)
								{
									list2.Add(diplomaticTermResourceExchange3);
									break;
								}
								num13 += 1f * num12;
								if (num13 > num10)
								{
									list2.Add(diplomaticTermResourceExchange3);
								}
							}
							if (list2.Count > 0 && layer.AnalyseContractProposition(list2, this.empire) + num3 > 0.1f)
							{
								if (num2 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num2 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
								{
									flag = true;
									break;
								}
								list2.Clear();
								list2.TrimExcess();
							}
						}
					}
				}
				diplomaticTerm6 = null;
			}
			enumerator2 = default(List<DiplomaticTerm>.Enumerator);
		}
		if (!flag)
		{
			foreach (DiplomaticTerm diplomaticTerm10 in list)
			{
				DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = diplomaticTerm10 as DiplomaticTermTechnologyExchange;
				if (diplomaticTermTechnologyExchange != null && diplomaticTermTechnologyExchange.CanApply(diplomaticContract, new string[0]))
				{
					yield return null;
					bool flag4 = false;
					foreach (DiplomaticTerm diplomaticTerm11 in this.currentDiplomaticTerms)
					{
						DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange2 = diplomaticTerm11 as DiplomaticTermTechnologyExchange;
						if (diplomaticTermTechnologyExchange2 != null && diplomaticTermTechnologyExchange2.Equals(diplomaticTermTechnologyExchange))
						{
							flag4 = true;
							break;
						}
					}
					if (!flag4 && DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition) <= 5 && layer.EvaluateDecision(diplomaticTermTechnologyExchange, this.empire, null).Score > 0.1f)
					{
						list2.Add(diplomaticTermTechnologyExchange);
						if (layer.AnalyseContractProposition(list2, this.empire) + num3 > 0.1f)
						{
							if (num2 + this.AnalyseContractProposition(list2, empire) > 0f || Mathf.Abs(num2 + this.AnalyseContractProposition(list2, empire)) <= this.SweetenDealThreshold * num)
							{
								flag = true;
								break;
							}
							list2.Clear();
							list2.TrimExcess();
						}
					}
				}
				diplomaticTermTechnologyExchange = null;
			}
			enumerator2 = default(List<DiplomaticTerm>.Enumerator);
		}
		if (flag)
		{
			this.currentDiplomaticTerms.AddRange(list2);
			foreach (DiplomaticTerm diplomaticTerm12 in this.currentDiplomaticTerms)
			{
				if (diplomaticTerm12 is DiplomaticTermBoosterExchange)
				{
					foreach (GameEntityGUID gameEntityGUID in (diplomaticTerm12 as DiplomaticTermBoosterExchange).BoosterGUID)
					{
						this.ResourceManager.BoostersInUse.Add(gameEntityGUID.ToString());
					}
				}
			}
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: Empire {0}/{1} with Empire {2}/{3} SweetenDeal succesfull", new object[]
				{
					base.AIEntity.Empire.Index,
					this.AnalyseContractProposition(this.currentDiplomaticTerms, empire),
					empire.ToString(),
					layer.AnalyseContractProposition(this.currentDiplomaticTerms, this.empire)
				});
			}
		}
		yield break;
		yield break;
	}

	private IEnumerator FillContractCoroutine(DiplomaticContract diplomaticContract)
	{
		if (diplomaticContract == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		if (this.currentDiplomaticTerms == null)
		{
			throw new ArgumentNullException("diplomaticTerms");
		}
		global::Empire empire = (diplomaticContract.EmpireWhichProposes.Index != this.empire.Index) ? diplomaticContract.EmpireWhichProposes : diplomaticContract.EmpireWhichReceives;
		AILayer_Diplomacy layer = null;
		AILayer_ResourceManager othersResourceManager = null;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(empire as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity != null)
			{
				layer = entity.GetLayer<AILayer_Diplomacy>();
				othersResourceManager = entity.GetLayer<AILayer_ResourceManager>();
			}
		}
		bool flag = false;
		bool flag2 = false;
		global::Empire thirdParty = null;
		foreach (DiplomaticTerm diplomaticTerm in diplomaticContract.Terms)
		{
			DiplomaticTermProposal diplomaticTermProposal = diplomaticTerm as DiplomaticTermProposal;
			if (diplomaticTermProposal != null && diplomaticTermProposal.Definition.Name == DiplomaticTermDefinition.Names.AskToDeclareWar)
			{
				thirdParty = diplomaticTermProposal.ChosenEmpire;
			}
		}
		DepartmentOfForeignAffairs agency = empire.GetAgency<DepartmentOfForeignAffairs>();
		Diagnostics.Assert(this.diplomaticTermsCacheList != null);
		this.diplomaticTermsCacheList.Clear();
		if (this.contructibleElementDatabase != null)
		{
			DepartmentOfForeignAffairs.ConstructibleElement[] values = this.contructibleElementDatabase.GetValues();
			if (values != null)
			{
				for (int i = 0; i < values.Length; i++)
				{
					DiplomaticTermDefinition diplomaticTermDefinition = values[i] as DiplomaticTermDefinition;
					if (diplomaticTermDefinition != null && !(diplomaticTermDefinition is DiplomaticTermDiplomaticRelationStateDefinition))
					{
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, empire, ref this.diplomaticTermsCacheList);
						DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, empire, this.empire, ref this.diplomaticTermsCacheList);
					}
				}
			}
		}
		Diagnostics.Assert(this.availableTermEvaluationsCacheList != null);
		this.availableTermEvaluationsCacheList.Clear();
		for (int j = 0; j < this.diplomaticTermsCacheList.Count; j++)
		{
			DiplomaticTerm diplomaticTerm2 = this.diplomaticTermsCacheList[j];
			DiplomaticTermProposal diplomaticTermProposal2 = diplomaticTerm2 as DiplomaticTermProposal;
			if (diplomaticTermProposal2 != null && ((diplomaticTermProposal2.EmpireWhichProvides == this.empire && this.empire.GetPropertyValue(SimulationProperties.WarCount) == 0f) || (diplomaticTermProposal2.EmpireWhichProvides == empire && empire.GetPropertyValue(SimulationProperties.WarCount) == 0f)))
			{
				this.DecideIfKeepProposal(diplomaticTermProposal2, diplomaticContract, agency, thirdParty, ref flag, ref flag2);
			}
			DiplomaticTermCityExchange diplomaticTermCityExchange = diplomaticTerm2 as DiplomaticTermCityExchange;
			if (diplomaticTermCityExchange != null && diplomaticTermCityExchange.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
			{
				this.diplomaticTermsCacheList.RemoveAt(j);
				j--;
			}
			else
			{
				DiplomaticTermBoosterExchange diplomaticTermBoosterExchange = diplomaticTerm2 as DiplomaticTermBoosterExchange;
				if (diplomaticTermBoosterExchange != null && this.ModifyOrRemoveBoosterExchange(empire, othersResourceManager, ref diplomaticTermBoosterExchange))
				{
					this.diplomaticTermsCacheList.RemoveAt(j);
					j--;
				}
				else
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation = new AILayer_Diplomacy.TermEvaluation(diplomaticTerm2);
					DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm2 as DiplomaticTermResourceExchange;
					if (diplomaticTermResourceExchange != null)
					{
						float num22;
						float maximumAmount;
						this.GetDiplomaticTermAmountLimits(diplomaticTerm2, out num22, out maximumAmount);
						diplomaticTermResourceExchange.Amount = num22;
						termEvaluation.MinimumAmount = num22;
						termEvaluation.MaximumAmount = maximumAmount;
					}
					this.availableTermEvaluationsCacheList.Add(termEvaluation);
				}
			}
		}
		Diagnostics.Assert(this.diplomaticTermsCacheList.Count == this.availableTermEvaluationsCacheList.Count);
		EvaluationData<DiplomaticTerm, InterpreterContext> evaluationData = null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			evaluationData = new EvaluationData<DiplomaticTerm, InterpreterContext>();
			evaluationData.Turn = this.game.Turn;
		}
		for (int k = 0; k < this.diplomaticTermsCacheList.Count; k++)
		{
			AILayer_Diplomacy.TermEvaluation termEvaluation2 = this.availableTermEvaluationsCacheList[k];
			termEvaluation2.SetAmount(termEvaluation2.MinimumAmount);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				termEvaluation2.MinimumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, evaluationData).Score;
			}
			else
			{
				termEvaluation2.MinimumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, null).Score;
			}
			Diagnostics.Assert(!float.IsNaN(termEvaluation2.MinimumAmountScore), "Evaluation of term '{0}' return a NaN value.", new object[]
			{
				termEvaluation2.Term
			});
			termEvaluation2.SetAmount(termEvaluation2.MaximumAmount);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				termEvaluation2.MaximumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, evaluationData).Score;
			}
			else
			{
				termEvaluation2.MaximumAmountScore = this.EvaluateDecision(termEvaluation2.Term, empire, null).Score;
			}
			Diagnostics.Assert(!float.IsNaN(termEvaluation2.MaximumAmountScore), "Evaluation of term '{0}' return a NaN value.", new object[]
			{
				termEvaluation2.Term
			});
		}
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			this.DebugEvaluationsHistoric.Add(evaluationData);
		}
		yield return null;
		float num3 = this.AnalyseContractProposition(diplomaticContract);
		this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation t) => t.Term is DiplomaticTermTechnologyExchange && t.Term.EmpireWhichProvides == empire && t.MaximumAmountScore <= 0f);
		if (num3 > 0f && layer.IsActive())
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation t) => !(t.Term is DiplomaticTermResourceExchange) && layer.EvaluateDecision(t.Term, this.empire, null).Score <= 0f);
		}
		int num23 = 0;
		foreach (AILayer_Diplomacy.TermEvaluation termEvaluation3 in this.availableTermEvaluationsCacheList)
		{
			if (!(termEvaluation3.Term is DiplomaticTermFortressExchange) && !(termEvaluation3.Term is DiplomaticTermCityExchange) && !(termEvaluation3.Term is DiplomaticTermProposal) && termEvaluation3.Term.EmpireWhichProvides == this.empire && Mathf.Abs(termEvaluation3.MaximumAmountScore) > num3 / 6f)
			{
				num23++;
			}
		}
		if (!flag)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == this.empire && x.Term is DiplomaticTermProposal);
		}
		if (num23 > 5)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == this.empire && (x.Term is DiplomaticTermFortressExchange || x.Term is DiplomaticTermCityExchange));
		}
		num23 = 0;
		foreach (AILayer_Diplomacy.TermEvaluation termEvaluation4 in this.availableTermEvaluationsCacheList)
		{
			if (!(termEvaluation4.Term is DiplomaticTermFortressExchange) && !(termEvaluation4.Term is DiplomaticTermCityExchange) && !(termEvaluation4.Term is DiplomaticTermProposal) && termEvaluation4.Term.EmpireWhichProvides == empire && Mathf.Abs(termEvaluation4.MaximumAmountScore) > num3 / 6f)
			{
				num23++;
			}
		}
		if (!flag2)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == empire && x.Term is DiplomaticTermProposal);
		}
		if (num23 > 5)
		{
			this.availableTermEvaluationsCacheList.RemoveAll((AILayer_Diplomacy.TermEvaluation x) => x.Term.EmpireWhichProvides == empire && (x.Term is DiplomaticTermFortressExchange || x.Term is DiplomaticTermCityExchange || x.Term is DiplomaticTermProposal));
		}
		if (layer != null && layer.IsActive())
		{
			this.availableTermEvaluationsCacheList.Sort(delegate(AILayer_Diplomacy.TermEvaluation left, AILayer_Diplomacy.TermEvaluation right)
			{
				float num40 = left.MaximumAmountScore;
				float num41 = right.MaximumAmountScore;
				bool flag3 = false;
				bool flag4 = false;
				bool flag5 = false;
				bool flag6 = false;
				if (Mathf.Abs(num40) > Mathf.Abs(num3 / 6f))
				{
					if (num40 < 0f)
					{
						flag4 = true;
						num40 = -layer.EvaluateDecision(left.Term, this.empire, null).Score;
					}
					if (num40 > 0f)
					{
						flag3 = true;
						num40 += layer.EvaluateDecision(left.Term, this.empire, null).Score;
					}
				}
				if (Mathf.Abs(num41) > Mathf.Abs(num3 / 6f))
				{
					if (num41 < 0f)
					{
						flag6 = true;
						num41 = -layer.EvaluateDecision(right.Term, this.empire, null).Score;
					}
					if (num41 > 0f)
					{
						flag5 = true;
						num41 += layer.EvaluateDecision(right.Term, this.empire, null).Score;
					}
				}
				if (flag4 && !flag6)
				{
					return -1;
				}
				if (!flag4 && flag6)
				{
					return 1;
				}
				if (flag3 && !flag5)
				{
					return 1;
				}
				if (!flag3 && flag5)
				{
					return -1;
				}
				if ((flag4 && flag6) || (flag3 && flag5))
				{
					return num40.CompareTo(num41);
				}
				return left.MaximumAmountScore.CompareTo(right.MaximumAmountScore);
			});
		}
		else
		{
			this.availableTermEvaluationsCacheList.Sort((AILayer_Diplomacy.TermEvaluation left, AILayer_Diplomacy.TermEvaluation right) => left.MaximumAmountScore.CompareTo(right.MaximumAmountScore));
		}
		yield return null;
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && ELCPUtilities.UseELCPMultiThreading)
		{
			Diagnostics.Log("ELCP {0} with {1} initial score: {2}", new object[]
			{
				this.empire,
				empire,
				num3
			});
			foreach (AILayer_Diplomacy.TermEvaluation termEvaluation5 in this.availableTermEvaluationsCacheList)
			{
				Diagnostics.Log("ELCP {0} with {1} terms {2}, score {3}, other score: {4}", new object[]
				{
					this.empire,
					empire,
					termEvaluation5.Term.ToString(),
					termEvaluation5.MaximumAmountScore,
					(layer != null) ? layer.EvaluateDecision(termEvaluation5.Term, this.empire, null).Score.ToString() : "null"
				});
			}
		}
		float num19 = this.DiplomacyFocus ? (this.maximumContractPropositionEvaluationScore / 2f) : this.maximumContractPropositionEvaluationScore;
		Interval otherInterval = new Interval(this.minimumContractPropositionEvaluationScore, num19);
		Interval interval = new Interval(num3, num3);
		while (this.currentDiplomaticTerms.Count < 7 && this.availableTermEvaluationsCacheList.Count > 0 && !interval.Intersect(otherInterval))
		{
			AILayer_Diplomacy.TermEvaluation termEvaluation6 = null;
			if (interval.IsGreaterThan(otherInterval))
			{
				for (int l = 0; l < this.availableTermEvaluationsCacheList.Count; l++)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation7 = this.availableTermEvaluationsCacheList[l];
					if (termEvaluation7.LowerScore <= 0f)
					{
						Interval interval2 = new Interval(interval.LowerBound + termEvaluation7.LowerScore, interval.UpperBound + termEvaluation7.GreaterScore);
						if (!interval2.IsLowerThan(otherInterval))
						{
							termEvaluation6 = termEvaluation7;
							break;
						}
					}
				}
			}
			else if (interval.IsLowerThan(otherInterval))
			{
				for (int m = this.availableTermEvaluationsCacheList.Count - 1; m >= 0; m--)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation8 = this.availableTermEvaluationsCacheList[m];
					if (termEvaluation8.GreaterScore >= 0f)
					{
						Interval interval3 = new Interval(interval.LowerBound + termEvaluation8.LowerScore, interval.UpperBound + termEvaluation8.GreaterScore);
						if (!interval3.IsGreaterThan(otherInterval))
						{
							termEvaluation6 = termEvaluation8;
							break;
						}
					}
				}
			}
			if (termEvaluation6 == null)
			{
				break;
			}
			this.currentDiplomaticTerms.Add(termEvaluation6.Term);
			this.availableTermEvaluationsCacheList.Remove(termEvaluation6);
			interval = new Interval(interval.LowerBound + termEvaluation6.LowerScore, interval.UpperBound + termEvaluation6.GreaterScore);
		}
		if (interval.UpperBound > interval.LowerBound)
		{
			num3 = this.AnalyseContractProposition(this.currentDiplomaticTerms, empire);
			if (num3 < this.minimumContractPropositionEvaluationScore || num3 > num19)
			{
				if (interval.UpperBound <= 0f)
				{
					for (int n = 0; n < this.currentDiplomaticTerms.Count; n++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = this.currentDiplomaticTerms[n] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange2 != null)
						{
							float num24;
							float amount;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange2, out num24, out amount);
							diplomaticTermResourceExchange2.Amount = amount;
						}
					}
					yield break;
				}
				if (interval.LowerBound >= 0f)
				{
					for (int num25 = 0; num25 < this.currentDiplomaticTerms.Count; num25++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = this.currentDiplomaticTerms[num25] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange3 != null)
						{
							float amount2;
							float num26;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange3, out amount2, out num26);
							diplomaticTermResourceExchange3.Amount = amount2;
						}
					}
					yield break;
				}
				if (interval.UpperBound > 0f && interval.LowerBound < 0f)
				{
					int num20 = 0;
					float num21 = 0f;
					while ((num3 < this.minimumContractPropositionEvaluationScore || num3 > num19) && num20 < this.currentDiplomaticTerms.Count)
					{
						yield return null;
						DiplomaticTermResourceExchange diplomaticTermResourceExchange4 = this.currentDiplomaticTerms[num20] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange4 == null)
						{
							int num27 = num20;
							num20 = num27 + 1;
						}
						else
						{
							float num28;
							float num29;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange4, out num28, out num29);
							float num30 = num29 - num28;
							DecisionResult decisionResult = this.EvaluateDecision(diplomaticTermResourceExchange4, empire, null);
							if ((num3 < this.minimumContractPropositionEvaluationScore && decisionResult.Score > 0f) || (num3 > num19 && decisionResult.Score < 0f))
							{
								if (num21 < 0f)
								{
									int num31 = num20;
									num20 = num31 + 1;
									continue;
								}
								float num32 = Mathf.Max(0.1f * num30, 1f);
								float num33 = Mathf.Floor(diplomaticTermResourceExchange4.Amount + num32);
								if (num33 >= num29)
								{
									diplomaticTermResourceExchange4.Amount = num29;
									int num34 = num20;
									num20 = num34 + 1;
									continue;
								}
								num21 = num32;
								diplomaticTermResourceExchange4.Amount = num33;
							}
							else if ((num3 < this.minimumContractPropositionEvaluationScore && decisionResult.Score < 0f) || (num3 > num19 && decisionResult.Score > 0f))
							{
								if (num21 > 0f)
								{
									int num35 = num20;
									num20 = num35 + 1;
									continue;
								}
								float num36 = Mathf.Max(0.1f * num30, 1f);
								float num37 = Mathf.Floor(diplomaticTermResourceExchange4.Amount - num36);
								if (num37 <= num28)
								{
									diplomaticTermResourceExchange4.Amount = num28;
									int num38 = num20;
									num20 = num38 + 1;
									continue;
								}
								num21 = -num36;
								diplomaticTermResourceExchange4.Amount = num37;
							}
							num3 = this.AnalyseContractProposition(this.currentDiplomaticTerms, empire);
						}
					}
				}
			}
		}
		using (List<DiplomaticTerm>.Enumerator enumerator3 = this.currentDiplomaticTerms.GetEnumerator())
		{
			while (enumerator3.MoveNext())
			{
				DiplomaticTerm diplomaticTerm3 = enumerator3.Current;
				if (diplomaticTerm3 is DiplomaticTermBoosterExchange)
				{
					foreach (GameEntityGUID gameEntityGUID in (diplomaticTerm3 as DiplomaticTermBoosterExchange).BoosterGUID)
					{
						this.ResourceManager.BoostersInUse.Add(gameEntityGUID.ToString());
					}
				}
			}
			yield break;
		}
		yield break;
	}

	public bool Update()
	{
		if (!this.coroutineActive)
		{
			return false;
		}
		this.currentContractCoroutine.Run();
		if (this.currentContractCoroutine.IsFinished)
		{
			this.coroutineActive = false;
			this.currentContractCoroutine = null;
			return false;
		}
		return true;
	}

	public override bool CanEndTurn()
	{
		return !this.coroutineActive;
	}

	private void GenerateContractRequests_TryImmediateActions(MajorEmpire opponentEmpire, WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage, int turnsSinceLastContract)
	{
		bool flag = false;
		if (this.empire.SimulationObject.Tags.Contains(AILayer_Diplomacy.ForceDiplomacyTrait) && this.ContractRequests.Count == 0 && turnsSinceLastContract % this.minimumNumberOfTurnsBetweenPropositions == 1 && wantedDiplomaticRelationStateMessage != null && wantedDiplomaticRelationStateMessage.State != BlackboardMessage.StateValue.Message_Canceled && !this.NeedsVictoryReaction[opponentEmpire.Index])
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
			Diagnostics.Assert(diplomaticRelation != null);
			if (diplomaticRelation.State != null && diplomaticRelation.State.Name != wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName && wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName != DiplomaticRelationState.Names.War && wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName != DiplomaticRelationState.Names.ColdWar)
			{
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
				{
					this.TryGenerateForceRequest(opponentEmpire, DiplomaticRelationState.Names.Truce);
					flag = true;
				}
				if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.ColdWar || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace)
				{
					this.TryGenerateForceRequest(opponentEmpire, wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName);
					flag = true;
				}
			}
		}
		if ((!flag || this.ContractRequests.Count == 0) && this.empire.GetPropertyValue(SimulationProperties.WarCount) > 0f && turnsSinceLastContract == 1)
		{
			turnsSinceLastContract = -1;
			Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index) || (contract.EmpireWhichProposes.Index == opponentEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
			foreach (DiplomaticContract diplomaticContract in this.diplomacyContractRepositoryService.FindAll(match))
			{
				if (diplomaticContract.State == DiplomaticContractState.Signed)
				{
					if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState && (term.Definition as DiplomaticTermDiplomaticRelationStateDefinition).DiplomaticRelationStateReference == DiplomaticRelationState.Names.Peace))
					{
						turnsSinceLastContract = Mathf.Max(turnsSinceLastContract, diplomaticContract.TurnAtTheBeginningOfTheState);
					}
					else if (this.MilitaryPowerDif < 0f)
					{
						if (diplomaticContract.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState && (term.Definition as DiplomaticTermDiplomaticRelationStateDefinition).DiplomaticRelationStateReference == DiplomaticRelationState.Names.Alliance))
						{
							turnsSinceLastContract = Mathf.Max(turnsSinceLastContract, diplomaticContract.TurnAtTheBeginningOfTheState);
						}
					}
				}
			}
			if (this.game.Turn - turnsSinceLastContract == 1)
			{
				this.AlwaysProcess = true;
				this.GenerateAskToDeclareWarContractRequest(opponentEmpire);
				this.AlwaysProcess = false;
			}
		}
	}

	private bool GenerateContractRequests_TryVictoryReactions(MajorEmpire opponentEmpire, WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage, DiplomaticRelation currentRelation)
	{
		bool flag = false;
		if (!this.opportunistThisTurn && Array.IndexOf<int>(AILayer_Diplomacy.VictoryTargets, opponentEmpire.Index) < 0 && AILayer_Diplomacy.VictoryTargets[this.empire.Index] < 0 && !this.canNeverDeclareWar)
		{
			if (this.NeedsVictoryReaction[opponentEmpire.Index] && currentRelation.State.Name != DiplomaticRelationState.Names.War && currentRelation.State.Name != DiplomaticRelationState.Names.Unknown)
			{
				flag = this.TryVictoryAlertDeclaration(opponentEmpire);
			}
			if (AILayer_Diplomacy.VictoryTargets[opponentEmpire.Index] < 0 && !flag && this.AnyVictoryreactionNeeded && (currentRelation.State.Name == DiplomaticRelationState.Names.Peace || currentRelation.State.Name == DiplomaticRelationState.Names.Alliance))
			{
				flag = this.TryVictoryAlertGangUp(opponentEmpire);
			}
		}
		return flag;
	}

	private void GenerateContractRequests_TryStandardRelationstateChange(MajorEmpire opponentEmpire, WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage, DiplomaticRelation currentRelation)
	{
		if (wantedDiplomaticRelationStateMessage != null && wantedDiplomaticRelationStateMessage.State != BlackboardMessage.StateValue.Message_Canceled && currentRelation.State != null && currentRelation.State.Name != wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName && (this.MilitaryPowerDif > 0f || (!(wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War) && ((!(currentRelation.State.Name == DiplomaticRelationState.Names.Peace) && !(currentRelation.State.Name == DiplomaticRelationState.Names.Alliance)) || !(wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName != DiplomaticRelationState.Names.Peace) || !(wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName != DiplomaticRelationState.Names.Alliance)))))
		{
			if (wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War)
			{
				if (!this.opportunistThisTurn && wantedDiplomaticRelationStateMessage.CurrentWarStatusType == AILayer_War.WarStatusType.Ready)
				{
					this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.War);
					return;
				}
				if (currentRelation.State.Name == DiplomaticRelationState.Names.Peace || currentRelation.State.Name == DiplomaticRelationState.Names.Alliance)
				{
					this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
					return;
				}
			}
			else
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName);
			}
		}
	}

	private void GenerateContractRequests_TryForceTruce(MajorEmpire opponentEmpire, int turnsSinceLastDeclarationByMe)
	{
		if (this.ContractRequests.Count == 0 && this.game.Turn - turnsSinceLastDeclarationByMe > 8 && this.empire.SimulationObject.Tags.Contains(AILayer_Diplomacy.ForceDiplomacyTrait) && this.departmentOfForeignAffairs.IsAtWarWith(opponentEmpire) && !this.NeedsVictoryReaction[opponentEmpire.Index] && opponentEmpire.GetPropertyValue(SimulationProperties.WarCount) < 2f && this.MilitaryPowerDif < 0f && Mathf.Abs(this.MilitaryPowerDif) > this.empire.GetPropertyValue(SimulationProperties.LandMilitaryPower) * 0.2f)
		{
			this.AlwaysProcess = true;
			this.TryGenerateForceRequest(opponentEmpire, DiplomaticRelationState.Names.Truce);
			this.AlwaysProcess = false;
		}
	}

	private void GenerateContractRequests_TryELCPDeals(MajorEmpire opponentEmpire, DiplomaticRelation currentRelation, int turnsSinceLastDeal, int turnsSinceLastMimicsCityDeal, int turnsSinceLastPrisonerDeal)
	{
		if (currentRelation.State != null)
		{
			if (currentRelation.State.Name == DiplomaticRelationState.Names.Peace || currentRelation.State.Name == DiplomaticRelationState.Names.Alliance)
			{
				if (this.game.Turn - turnsSinceLastDeal > 14 || (this.DiplomacyFocus && this.game.Turn - turnsSinceLastDeal > 9))
				{
					this.DealIndeces.Add(opponentEmpire.Index);
				}
				if (this.canInfect && !this.departmentOfPlanificationAndDevelopment.HasIntegratedFaction(opponentEmpire.Faction) && this.game.Turn - turnsSinceLastMimicsCityDeal > 20)
				{
					this.MimicCityDealIndeces.Add(opponentEmpire.Index);
				}
			}
			if (currentRelation.State.Name != DiplomaticRelationState.Names.War && currentRelation.State.Name != DiplomaticRelationState.Names.Unknown && this.game.Turn - turnsSinceLastPrisonerDeal > 11)
			{
				this.PrisonDealIndeces.Add(opponentEmpire.Index);
			}
		}
	}

	private void GenerateContractRequests_ComputeLastContractTurns(MajorEmpire opponentEmpire, out int turnOfLastContractInitiatedByMe, out int turnOfLastContract, out int turnOfLastELCPDeal, out int turnOfLastELCPPrisonerDeal, out int turnOfLastELCPMimicsCityDeal, out int turnOfLastDeclarationByMe)
	{
		turnOfLastContractInitiatedByMe = -1;
		turnOfLastContract = 0;
		turnOfLastELCPDeal = 0;
		turnOfLastELCPPrisonerDeal = 0;
		turnOfLastELCPMimicsCityDeal = 0;
		turnOfLastDeclarationByMe = 0;
		Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index) || (contract.EmpireWhichProposes.Index == opponentEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
		foreach (DiplomaticContract diplomaticContract in this.diplomacyContractRepositoryService.FindAll(match))
		{
			if (diplomaticContract.State != DiplomaticContractState.Negotiation)
			{
				turnOfLastContract++;
				if (diplomaticContract.EmpireWhichInitiated.Index == this.empire.Index)
				{
					turnOfLastContractInitiatedByMe = Mathf.Max(turnOfLastContractInitiatedByMe, diplomaticContract.TurnAtTheBeginningOfTheState);
					List<DiplomaticTerm> list = diplomaticContract.Terms.ToList<DiplomaticTerm>();
					if (diplomaticContract.State == DiplomaticContractState.Refused || diplomaticContract.State == DiplomaticContractState.Ignored)
					{
						bool flag = diplomaticContract.TurnAtTheBeginningOfTheState <= turnOfLastELCPDeal;
						bool flag2 = diplomaticContract.TurnAtTheBeginningOfTheState <= turnOfLastELCPPrisonerDeal;
						bool flag3 = !this.canInfect || diplomaticContract.TurnAtTheBeginningOfTheState <= turnOfLastELCPMimicsCityDeal;
						foreach (DiplomaticTerm diplomaticTerm in list)
						{
							if (!flag && (diplomaticTerm is DiplomaticTermResourceExchange || diplomaticTerm is DiplomaticTermTechnologyExchange) && diplomaticTerm.EmpireWhichReceives.Index == this.empire.Index)
							{
								turnOfLastELCPDeal = diplomaticContract.TurnAtTheBeginningOfTheState;
								flag = true;
							}
							else if (!flag2 && diplomaticTerm is DiplomaticTermPrisonerExchange)
							{
								turnOfLastELCPPrisonerDeal = diplomaticContract.TurnAtTheBeginningOfTheState;
								flag2 = true;
							}
							else if (!flag3 && diplomaticTerm is DiplomaticTermCityExchange && diplomaticTerm.Definition.Name == DiplomaticTermCityExchange.MimicsCityDeal)
							{
								turnOfLastELCPMimicsCityDeal = diplomaticContract.TurnAtTheBeginningOfTheState;
								flag3 = true;
							}
						}
					}
					if (diplomaticContract.State == DiplomaticContractState.Signed && diplomaticContract.GetPropositionMethod() == DiplomaticTerm.PropositionMethod.Declaration && diplomaticContract.EmpireWhichInitiated.Index == this.empire.Index)
					{
						foreach (DiplomaticTerm diplomaticTerm2 in list)
						{
							DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = diplomaticTerm2.Definition as DiplomaticTermDiplomaticRelationStateDefinition;
							if (diplomaticTermDiplomaticRelationStateDefinition != null && diplomaticTermDiplomaticRelationStateDefinition.DiplomaticRelationStateReference == DiplomaticRelationState.Names.War && diplomaticContract.TurnAtTheBeginningOfTheState > turnOfLastDeclarationByMe)
							{
								turnOfLastDeclarationByMe = diplomaticContract.TurnAtTheBeginningOfTheState;
								break;
							}
						}
					}
				}
			}
		}
	}

	private bool GenerateContractRequests_TryOpportunistWarDeclaration(MajorEmpire opponentEmpire, WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage, DiplomaticRelation currentRelation)
	{
		bool result = false;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!this.opportunistThisTurn && currentRelation.State.Name != DiplomaticRelationState.Names.Unknown && !this.departmentOfForeignAffairs.IsInWarWithSomeone() && !this.canNeverDeclareWar && opponentEmpire.GetPropertyValue(SimulationProperties.WarCount) > 0f && !this.GetPeaceWish(opponentEmpire.Index) && wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War && (this.aiLayerDiplomacyAmas.GetAgentGroupForEmpire(opponentEmpire).GetAgent(AILayer_DiplomacyAmas.AgentNames.WarTermAgent) as WarTermAgent).IsOpportunist && this.aIScheduler != null && this.aIScheduler.TryGetMajorEmpireAIPlayer(opponentEmpire, out aiplayer_MajorEmpire))
		{
			List<Army> list = this.departmentOfDefense.Armies.ToList<Army>().FindAll((Army match) => !match.IsSeafaring && !match.IsSettler && !match.IsSolitary && match.UnitsCount > 2);
			if (list.Count > 2)
			{
				if (list.FindAll((Army match) => match.StandardUnits.Count == match.MaximumUnitSlot).Count > 0)
				{
					AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
					if (entity != null)
					{
						AILayer_Diplomacy layer = entity.GetLayer<AILayer_Diplomacy>();
						if (this.GetMilitaryPowerDif(false) > layer.GetMilitaryPowerDif(false) * 1.5f && this.GetMilitaryPowerDif(false) > 0.33f * opponentEmpire.GetPropertyValue(SimulationProperties.LandMilitaryPower))
						{
							this.AlwaysProcess = true;
							this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.War);
							this.AlwaysProcess = false;
							result = true;
						}
					}
				}
			}
		}
		return result;
	}

	private const bool DisableForceStatus = false;

	public const string AIDiplomacyRegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy/";

	private List<DecisionResult> decisions;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private Dictionary<StaticString, IAIParameter<InterpreterContext>[]> aiParametersByElement;

	public List<AILayer_Diplomacy.ContractRequest> ContractRequests;

	[InfluencedByPersonality]
	private int minimumNumberOfTurnsBetweenPropositions;

	[InfluencedByPersonality]
	private int maximumNumberOfTurnsBetweenPropositions;

	[InfluencedByPersonality]
	private int maximumNumberOfTurnsBetweenStatusAndForceStatus;

	private AILayer_Diplomacy.ContractRequest currentContractRequest;

	public FixedSizedList<EvaluationData<DiplomaticTerm, InterpreterContext>> DebugEvaluationsHistoric;

	private ElementEvaluator<DiplomaticTerm, InterpreterContext> diplomaticTermEvaluator;

	private AILayer_Research aiLayerResearch;

	private List<AILayer_Diplomacy.TermEvaluation> availableTermEvaluationsCacheList;

	private List<DiplomaticTerm> diplomaticTermsCacheList;

	private global::Empire lastEmpireInInterpreter;

	[InfluencedByPersonality]
	private float diplomaticRelationStateAgentCriticityThreshold;

	[InfluencedByPersonality]
	private float diplomaticTermAgentCriticityThreshold;

	[InfluencedByPersonality]
	private float diplomaticRelationStateAgentCriticityEpsilon;

	[InfluencedByPersonality]
	private float maximumContractPropositionEvaluationScore;

	[InfluencedByPersonality]
	private float minimumContractPropositionEvaluationScore;

	[InfluencedByPersonality]
	private float maximumExchangeAmountRatio;

	[InfluencedByPersonality]
	private float minimumExchangeAmountRatio;

	private AILayer_AccountManager aiLayerAccountManager;

	private AILayer_Attitude aiLayerAttitude;

	private AILayer_DiplomacyAmas aiLayerDiplomacyAmas;

	private AILayer_ResourceAmas aiLayerResourceAmas;

	private AILayer_Navy aiLayerNavy;

	private IDatabase<AIParameterDatatableElement> aiParameterDatabase;

	private IDatabase<DepartmentOfForeignAffairs.ConstructibleElement> contructibleElementDatabase;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfPlanificationAndDevelopment departmentOfPlanificationAndDevelopment;

	private DepartmentOfScience departmentOfScience;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private IDiplomaticContractRepositoryService diplomacyContractRepositoryService;

	private IDiplomacyService diplomacyService;

	private float diplomaticMaximumAccountMultiplier;

	private Dictionary<StaticString, DiplomaticRelationState> diplomaticRelationStateByAmasAgentName;

	private IDatabase<DiplomaticRelationState> diplomaticRelationStateDatabase;

	private Dictionary<StaticString, DiplomaticTermDefinition[]> diplomaticTermByAmasAgentName;

	private DiplomaticTermDefinition diplomaticTermGratify;

	private DiplomaticTermDefinition diplomaticTermWarning;

	private global::Empire empire;

	private global::Game game;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private MajorEmpire[] majorEmpires;

	private StaticString[] mostWantedDiplomaticTermAgentNameByEmpireIndex;

	private Dictionary<StaticString, DiplomaticTermDefinition[]> proposalTermByAmasAgentName;

	private ITickableRepositoryAIHelper tickableRepositoryAIHelper;

	private IWorldPositionningService worldPositionningService;

	private IEventService eventService;

	[InfluencedByPersonality]
	private int wantedDiplomaticRelationStateMessageRevaluationPeriod;

	private float maximumNumberDiplomaticProposalsPerTurn;

	private float SweetenDealThreshold;

	public bool AlwaysProcess;

	private float EmergencyGangupLimit;

	private DiplomaticContract CurrentAnswerContract;

	private List<int> AlreadyContactedEmpires;

	public bool[] NeedsVictoryReaction;

	private bool SharedVictory;

	private float MilitaryPowerDif;

	private List<int> DealIndeces;

	private int GameDifficulty;

	public bool AnyVictoryreactionNeeded;

	private static int[] VictoryTargets = new int[]
	{
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1,
		-1
	};

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private AILayer_Victory VictoryLayer;

	private bool DiplomacyFocus;

	private DepartmentOfEducation departmentOfEducation;

	private List<int> PrisonDealIndeces;

	private AILayer_ResourceManager ResourceManager;

	private bool[] PeaceWish;

	private List<int> MimicCityDealIndeces;

	private bool canInfect;

	private static StaticString ForceDiplomacyTrait = "FactionTraitDrakkens6";

	private int lastUpdate;

	private bool canNeverDeclareWar;

	private List<DiplomaticTerm> currentDiplomaticTerms;

	private Amplitude.Coroutine currentContractCoroutine;

	private bool coroutineActive;

	private AIScheduler aIScheduler;

	private float resourceWarReserveFactor;

	private static StaticString FactionTraitTechnologyDefinitionOrbUnlock18WinterShifters = "FactionTraitTechnologyDefinitionOrbUnlock18WinterShifters";

	public static StaticString UnlockedTechnologyCount = "UnlockedTechnologyCount";

	private static StaticString ClassResearch = "ClassResearch";

	private bool opportunistThisTurn;

	private DepartmentOfDefense departmentOfDefense;

	private static class EvaluationCategory
	{
		public const string UtilityProvider = "DiplomacyUtilityProvider";

		public const string UtilityReceiver = "DiplomacyUtilityReceiver";

		public const string EconomyProvider = "DiplomacyEconomyProvider";

		public const string EconomyReceiver = "DiplomacyEconomyReceiver";

		public const string TechnologyProvider = "DiplomacyTechnologyProvider";

		public const string TechnologyReceiver = "DiplomacyTechnologyReceiver";

		public const string MilitarySupportProvider = "DiplomacyMilitarySupportProvider";

		public const string MilitarySupportReceiver = "DiplomacyMilitarySupportReceiver";

		public const string OrbProvider = "DiplomacyOrbProvider";

		public const string OrbReceiver = "DiplomacyOrbReceiver";
	}

	public class ContractRequest
	{
		public ContractRequest(global::Empire initiatorEmpire, global::Empire opponentEmpire)
		{
			this.InitiatorEmpire = initiatorEmpire;
			this.OpponentEmpire = opponentEmpire;
			this.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeScored;
		}

		public override string ToString()
		{
			return string.Format("ContractRequest From empire {0} to empire {1} State: {2}", this.InitiatorEmpire.Index, this.OpponentEmpire.Index, this.State);
		}

		public DiplomaticContract Contract;

		public ulong CurrentOrderTicketNumber;

		public global::Empire InitiatorEmpire;

		public global::Empire OpponentEmpire;

		public AILayer_Diplomacy.ContractRequest.ContractRequestState State;

		public List<DiplomaticTerm> Terms = new List<DiplomaticTerm>();

		public enum ContractRequestState
		{
			WaitingToBeScored,
			WaitingToBeProcessed,
			RetrieveContract,
			PreFillContract,
			FillContract,
			ProposeContract,
			Done,
			Failed
		}
	}

	private class TermEvaluation
	{
		public TermEvaluation(DiplomaticTerm term)
		{
			this.Term = term;
			this.MinimumAmountScore = 0f;
			this.MaximumAmountScore = 0f;
			this.MinimumAmount = 1f;
			this.MaximumAmount = 1f;
		}

		public float GreaterScore
		{
			get
			{
				return Mathf.Max(this.MinimumAmountScore, this.MaximumAmountScore);
			}
		}

		public float LowerScore
		{
			get
			{
				return Mathf.Min(this.MinimumAmountScore, this.MaximumAmountScore);
			}
		}

		public void SetAmount(float amount)
		{
			DiplomaticTermResourceExchange diplomaticTermResourceExchange = this.Term as DiplomaticTermResourceExchange;
			if (diplomaticTermResourceExchange != null)
			{
				diplomaticTermResourceExchange.Amount = amount;
			}
		}

		public float MaximumAmount;

		public float MaximumAmountScore;

		public float MinimumAmount;

		public float MinimumAmountScore;

		public DiplomaticTerm Term;
	}
}
