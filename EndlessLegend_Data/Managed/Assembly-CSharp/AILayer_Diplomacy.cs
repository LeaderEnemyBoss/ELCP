using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;
using Amplitude.Unity.Xml;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy/", new object[]
{

})]
public class AILayer_Diplomacy : AILayer, ITickable, ISimulationAIEvaluationHelper<DiplomaticTerm>, IAIEvaluationHelper<DiplomaticTerm, InterpreterContext>
{
	public AILayer_Diplomacy()
	{
		this.decisions = new List<DecisionResult>();
		base..ctor();
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
		}
		else
		{
			float num = this.AnalyseContractProposition(diplomaticContract);
			if (num >= 0f)
			{
				this.aiLayerAttitude.RegisterContractBenefitForMyEmpire(diplomaticContract, num);
				this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Signed);
			}
			else
			{
				this.OrderChangeDiplomaticContractState(diplomaticContract, DiplomaticContractState.Refused);
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
	}

	private void RelationStateChangeToWar(global::Empire enemyEmpire)
	{
		float num = 5f;
		bool flag = false;
		for (int i = 0; i < this.game.Empires.Length; i++)
		{
			MajorEmpire majorEmpire = this.game.Empires[i] as MajorEmpire;
			if (majorEmpire != null && this.empire != majorEmpire)
			{
				float agentCriticityFor = this.GetAgentCriticityFor(majorEmpire, AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar);
				if (agentCriticityFor > 0.5f)
				{
					DepartmentOfForeignAffairs agency = majorEmpire.GetAgency<DepartmentOfForeignAffairs>();
					if (this.departmentOfForeignAffairs.DiplomaticRelations[i].State != null && agency.DiplomaticRelations[i].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.War && agency.DiplomaticRelations[i].State.Name != DiplomaticRelationState.Names.War)
					{
						bool flag2 = false;
						DiplomaticTermProposal proposal;
						foreach (KeyValuePair<ulong, DiplomaticContract> keyValuePair in this.diplomacyContractRepositoryService)
						{
							DiplomaticContract value = keyValuePair.Value;
							if (value.EmpireWhichProposes.Index == this.empire.Index && value.EmpireWhichReceives.Index == majorEmpire.Index)
							{
								if (value.State != DiplomaticContractState.Negotiation)
								{
									if ((float)(this.game.Turn - value.TurnAtTheBeginningOfTheState) > num)
									{
										break;
									}
									for (int j = 0; j < value.Terms.Count; j++)
									{
										proposal = (value.Terms[j] as DiplomaticTermProposal);
										if (proposal != null && Array.Exists<DiplomaticTermDefinition>(this.proposalTermByAmasAgentName[AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar], (DiplomaticTermDefinition match) => match.Name == proposal.ProposedTerm.Definition.Name))
										{
											flag2 = true;
											break;
										}
									}
									if (flag2)
									{
										break;
									}
								}
							}
						}
						if (!flag2)
						{
							StaticString askToDeclareWar = AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar;
							if (this.TryGenerateAskToDiplomaticTerm(majorEmpire, enemyEmpire, askToDeclareWar, out proposal) && this.EvaluateDecision(proposal, majorEmpire, null).Score > 0f)
							{
								AILayer_Diplomacy.ContractRequest contractRequest = this.GenerateContractRequest_AskTo(majorEmpire, enemyEmpire, askToDeclareWar);
								if (contractRequest != null)
								{
									this.ContractRequests.Add(contractRequest);
									flag = true;
								}
							}
						}
					}
				}
			}
		}
		if (flag)
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
		this.GenerateAskToDeclareWarContractRequest(opponentEmpire);
		this.GenerateAskToBlackSpotContractRequest(opponentEmpire);
	}

	private void GenerateAskToDeclareWarContractRequest(MajorEmpire opponentEmpire)
	{
		StaticString askToDeclareWar = AILayer_DiplomacyAmas.AgentNames.AskToDeclareWar;
		float agentCriticityFor = this.GetAgentCriticityFor(opponentEmpire, askToDeclareWar);
		if (agentCriticityFor > 0.5f)
		{
			List<int> list = new List<int>();
			DepartmentOfForeignAffairs agency = opponentEmpire.GetAgency<DepartmentOfForeignAffairs>();
			for (int i = 0; i < this.departmentOfForeignAffairs.DiplomaticRelations.Count; i++)
			{
				if (this.departmentOfForeignAffairs.DiplomaticRelations[i].State != null && agency.DiplomaticRelations[i].State != null && this.departmentOfForeignAffairs.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.War && agency.DiplomaticRelations[i].State.Name != DiplomaticRelationState.Names.War)
				{
					list.Add(i);
				}
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
						this.ContractRequests.Add(contractRequest);
					}
				}
			}
		}
	}

	private void GenerateAskToBlackSpotContractRequest(MajorEmpire opponentEmpire)
	{
		StaticString askToBlackSpotTermAgent = AILayer_DiplomacyAmas.AgentNames.AskToBlackSpotTermAgent;
		float agentCriticityFor = this.GetAgentCriticityFor(opponentEmpire, askToBlackSpotTermAgent);
		if (agentCriticityFor > 0.5f)
		{
			int num = -1;
			float num2 = 0f;
			DepartmentOfForeignAffairs agency = opponentEmpire.GetAgency<DepartmentOfForeignAffairs>();
			for (int i = 0; i < this.game.Empires.Length; i++)
			{
				MajorEmpire majorEmpire = this.game.Empires[i] as MajorEmpire;
				if (majorEmpire != null)
				{
					if (majorEmpire.Index != this.empire.Index && majorEmpire.Index != opponentEmpire.Index)
					{
						if (agency.DiplomaticRelations[i].State == null || (!(agency.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.Peace) && !(agency.DiplomaticRelations[i].State.Name == DiplomaticRelationState.Names.Alliance)))
						{
							float globalScore = this.GetGlobalScore(majorEmpire);
							if (globalScore > num2)
							{
								num = majorEmpire.Index;
							}
						}
					}
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
		Blackboard blackboard = base.AIEntity.AIPlayer.Blackboard;
		int num = -1;
		int num2 = 0;
		Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index) || (contract.EmpireWhichProposes.Index == opponentEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
		foreach (DiplomaticContract diplomaticContract in this.diplomacyContractRepositoryService.FindAll(match))
		{
			if (diplomaticContract.State != DiplomaticContractState.Negotiation)
			{
				num2++;
				if (diplomaticContract.EmpireWhichInitiated.Index == this.empire.Index)
				{
					num = Mathf.Max(num, diplomaticContract.TurnAtTheBeginningOfTheState);
				}
			}
		}
		int num3 = this.game.Turn - num;
		int num4 = this.minimumNumberOfTurnsBetweenPropositions + num2 * (base.AIEntity.Empire.Index + 1) % (this.maximumNumberOfTurnsBetweenPropositions - this.minimumNumberOfTurnsBetweenPropositions + 1);
		if (num3 < num4)
		{
			return;
		}
		WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == opponentEmpire.Index);
		if (wantedDiplomaticRelationStateMessage != null && wantedDiplomaticRelationStateMessage.State != BlackboardMessage.StateValue.Message_Canceled)
		{
			DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
			Diagnostics.Assert(diplomaticRelation != null);
			if (diplomaticRelation.State != null && diplomaticRelation.State.Name != wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName)
			{
				if (wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War)
				{
					if (wantedDiplomaticRelationStateMessage.CurrentWarStatusType == AILayer_War.WarStatusType.Ready)
					{
						this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.War);
					}
					else if (diplomaticRelation.State.Name == DiplomaticRelationState.Names.Peace || diplomaticRelation.State.Name == DiplomaticRelationState.Names.Alliance)
					{
						this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
					}
				}
				else
				{
					this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName);
				}
			}
		}
		this.GenerateAskToContractRequest(opponentEmpire);
		StaticString staticString = this.mostWantedDiplomaticTermAgentNameByEmpireIndex[opponentEmpire.Index];
		if (!StaticString.IsNullOrEmpty(staticString))
		{
			this.GenerateTreatyContractRequest(opponentEmpire, staticString);
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
		if (diplomaticTermDefinition == null)
		{
			return;
		}
		List<DiplomaticTerm> list = new List<DiplomaticTerm>();
		DiplomaticTermProposalDefinition diplomaticTermProposalDefinition = diplomaticTermDefinition as DiplomaticTermProposalDefinition;
		if (diplomaticTermProposalDefinition != null)
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
		for (int j = 0; j < list.Count; j++)
		{
			DiplomaticTerm diplomaticTerm = list[j];
			float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
			if (empirePointCost <= num)
			{
				AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
				Diagnostics.Assert(contractRequest.Terms != null);
				contractRequest.Terms.Add(diplomaticTerm);
				this.ContractRequests.Add(contractRequest);
				break;
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
		proposal = new DiplomaticTermProposal(diplomaticTermProposalDefinition, this.empire, this.empire, alliedEmpire);
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
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(proposal, this.empire);
		return empirePointCost <= num;
	}

	private void TryGenerateDiplomaticStateChangeContractRequest(MajorEmpire opponentEmpire, StaticString wantedDiplomaticRelationState)
	{
		DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(opponentEmpire);
		Diagnostics.Assert(diplomaticRelation != null && diplomaticRelation.State != null);
		StaticString name = diplomaticRelation.State.Name;
		DiplomaticContract diplomaticContract = new DiplomaticContract(GameEntityGUID.Zero, this.empire, opponentEmpire);
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition = null;
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition2 = null;
		IEnumerable<DiplomaticTermDiplomaticRelationStateDefinition> diplomaticTermDiplomaticRelationStateDefinition3 = this.departmentOfForeignAffairs.GetDiplomaticTermDiplomaticRelationStateDefinition(diplomaticContract, wantedDiplomaticRelationState);
		foreach (DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition4 in diplomaticTermDiplomaticRelationStateDefinition3)
		{
			Diagnostics.Assert(diplomaticTermDiplomaticRelationStateDefinition4 != null);
			if (diplomaticTermDiplomaticRelationStateDefinition == null || diplomaticTermDiplomaticRelationStateDefinition.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				diplomaticTermDiplomaticRelationStateDefinition = diplomaticTermDiplomaticRelationStateDefinition4;
			}
			if (diplomaticTermDiplomaticRelationStateDefinition4.PropositionMethod == DiplomaticTerm.PropositionMethod.Declaration)
			{
				diplomaticTermDiplomaticRelationStateDefinition2 = diplomaticTermDiplomaticRelationStateDefinition4;
			}
		}
		DiplomaticTermDiplomaticRelationStateDefinition diplomaticTermDiplomaticRelationStateDefinition5 = diplomaticTermDiplomaticRelationStateDefinition;
		Agent forceStatusAgentFromDiplomaticRelationState = this.GetForceStatusAgentFromDiplomaticRelationState(opponentEmpire, wantedDiplomaticRelationState);
		if (diplomaticTermDiplomaticRelationStateDefinition2 != null && forceStatusAgentFromDiplomaticRelationState != null && forceStatusAgentFromDiplomaticRelationState.Enable)
		{
			float intensity = forceStatusAgentFromDiplomaticRelationState.CriticityMax.Intensity;
			if (intensity >= this.diplomaticRelationStateAgentCriticityThreshold)
			{
				int num = -1;
				Predicate<DiplomaticContract> match = (DiplomaticContract contract) => (contract.EmpireWhichProposes.Index == this.empire.Index && contract.EmpireWhichReceives.Index == opponentEmpire.Index) || (contract.EmpireWhichProposes.Index == opponentEmpire.Index && contract.EmpireWhichReceives.Index == this.empire.Index);
				foreach (DiplomaticContract diplomaticContract2 in this.diplomacyContractRepositoryService.FindAll(match))
				{
					if (diplomaticContract2.State != DiplomaticContractState.Negotiation && diplomaticContract2.State != DiplomaticContractState.Signed)
					{
						if (diplomaticContract2.Terms.Any((DiplomaticTerm term) => term is DiplomaticTermDiplomaticRelationState && (term.Definition as DiplomaticTermDiplomaticRelationStateDefinition).DiplomaticRelationStateReference == wantedDiplomaticRelationState))
						{
							num = Mathf.Max(num, diplomaticContract2.TurnAtTheBeginningOfTheState);
						}
					}
				}
				if (num >= 0)
				{
					int num2 = this.game.Turn - num;
					if (num2 <= this.maximumNumberOfTurnsBetweenStatusAndForceStatus)
					{
						diplomaticTermDiplomaticRelationStateDefinition5 = diplomaticTermDiplomaticRelationStateDefinition2;
					}
				}
			}
		}
		if (diplomaticTermDiplomaticRelationStateDefinition5 == null)
		{
			if (name == DiplomaticRelationState.Names.ColdWar && wantedDiplomaticRelationState == DiplomaticRelationState.Names.Alliance)
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.Peace);
			}
			return;
		}
		DiplomaticTerm diplomaticTerm = new DiplomaticTermDiplomaticRelationState(diplomaticTermDiplomaticRelationStateDefinition5, this.empire, this.empire, opponentEmpire);
		float empirePointCost = DepartmentOfForeignAffairs.GetEmpirePointCost(diplomaticTerm, this.empire);
		this.WantedPrestigePoint = Mathf.Max(empirePointCost, this.WantedPrestigePoint);
		float num3;
		float num4;
		if (!this.aiLayerAccountManager.TryGetAccountInfos(AILayer_AccountManager.DiplomacyAccountName, DepartmentOfTheTreasury.Resources.EmpirePoint, out num3, out num4))
		{
			AILayer.LogError("Can't retrieve empire point account infos");
		}
		if (empirePointCost > num3)
		{
			if (name == DiplomaticRelationState.Names.Peace && wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
			}
			else if (name == DiplomaticRelationState.Names.Alliance && wantedDiplomaticRelationState == DiplomaticRelationState.Names.War)
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.ColdWar);
			}
			else if (name == DiplomaticRelationState.Names.Alliance && wantedDiplomaticRelationState == DiplomaticRelationState.Names.ColdWar)
			{
				this.TryGenerateDiplomaticStateChangeContractRequest(opponentEmpire, DiplomaticRelationState.Names.Peace);
			}
			else
			{
				this.GenerateDiscussionContractRequest(opponentEmpire, diplomaticTermDiplomaticRelationStateDefinition5.Alignment);
			}
			return;
		}
		AILayer_Diplomacy.ContractRequest contractRequest = new AILayer_Diplomacy.ContractRequest(this.empire, opponentEmpire);
		contractRequest.Terms.Add(diplomaticTerm);
		this.ContractRequests.Add(contractRequest);
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
		if (this.currentContractRequest != null && this.currentContractRequest.State != AILayer_Diplomacy.ContractRequest.ContractRequestState.Done && this.currentContractRequest.State != AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed)
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
		this.FillContract(contractRequest.Contract, ref contractRequest.Terms);
		Diagnostics.Assert(contractRequest.Contract != null);
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
			DiplomaticTerm term = contractRequest.Terms[j];
			array[count + j] = DiplomaticTermChange.Add(term);
		}
		OrderChangeDiplomaticContractTermsCollection orderChangeDiplomaticContractTermsCollection = new OrderChangeDiplomaticContractTermsCollection(contractRequest.Contract, array);
		Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
		Ticket ticket;
		base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractTermsCollection, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
		contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractTermsCollection.TicketNumber;
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
			AILayer.LogError("The order {0} failed.", new object[]
			{
				ticketRaisedEventArgs.Order
			});
			return;
		}
		switch (contractRequest.State)
		{
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.RetrieveContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderCreateDiplomaticContract);
			this.PreFillContract(contractRequest);
			break;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.PreFillContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractTermsCollection);
			this.OrderFillContract(contractRequest);
			break;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.FillContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractTermsCollection);
			this.ProposeContract(contractRequest);
			break;
		case AILayer_Diplomacy.ContractRequest.ContractRequestState.ProposeContract:
			Diagnostics.Assert(ticketRaisedEventArgs.Order is OrderChangeDiplomaticContractState);
			contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Done;
			break;
		default:
			AILayer.LogError("Contract request state invalid ({1}) when receiving order {0}", new object[]
			{
				ticketRaisedEventArgs.Order,
				contractRequest.State
			});
			break;
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
		if (this.aiLayerAccountManager.TryMakeUnexpectedImmediateExpense(AILayer_AccountManager.DiplomacyAccountName, empirePointCost, 0f))
		{
			OrderChangeDiplomaticContractState orderChangeDiplomaticContractState = new OrderChangeDiplomaticContractState(contractRequest.Contract, DiplomaticContractState.Proposed);
			Diagnostics.Assert(base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI);
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(orderChangeDiplomaticContractState, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OrderProcessedEventHandler));
			contractRequest.CurrentOrderTicketNumber = orderChangeDiplomaticContractState.TicketNumber;
		}
		else
		{
			AILayer.Log("The empire {0} tried to propose the following contract but has not enough empire point.\n{1}", new object[]
			{
				this.empire.Index,
				contractRequest.Contract
			});
			contractRequest.State = AILayer_Diplomacy.ContractRequest.ContractRequestState.Failed;
		}
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
		global::Empire empire = (!((float)empireWhichProvides.Index == num)) ? empireWhichReceives : empireWhichProvides;
		global::Empire empire2 = (!((float)empireWhichProvides.Index == num)) ? empireWhichProvides : empireWhichReceives;
		float empireWealth = AILayer_Diplomacy.GetEmpireWealth(empire);
		float empireWealth2 = AILayer_Diplomacy.GetEmpireWealth(empire2);
		float num2 = 1f / Mathf.Max(1f, Mathf.Min(empireWealth2, empireWealth));
		return aiParameterValue * num2;
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
		global::Empire empire = (!((float)empireWhichProvides.Index == num)) ? empireWhichReceives : empireWhichProvides;
		global::Empire empire2 = (!((float)empireWhichProvides.Index == num)) ? empireWhichProvides : empireWhichReceives;
		float empireWealth = AILayer_Diplomacy.GetEmpireWealth(empire);
		float empireWealth2 = AILayer_Diplomacy.GetEmpireWealth(empire2);
		float num2 = 1f / Mathf.Max(1f, Mathf.Max(empireWealth2, empireWealth));
		return aiParameterValue * num2;
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
		SimulationObject simulationObject3 = simulationObject.Children.Find((SimulationObject match) => match.Tags.Contains("ClassResearch"));
		float propertyValue = simulationObject3.GetPropertyValue("UnlockedTechnologyCount");
		simulationObject3 = simulationObject2.Children.Find((SimulationObject match) => match.Tags.Contains("ClassResearch"));
		float propertyValue2 = simulationObject3.GetPropertyValue("UnlockedTechnologyCount");
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
		SimulationObject simulationObject = context.Get("MyEmpire") as SimulationObject;
		SimulationObject simulationObject2 = context.Get("OtherEmpire") as SimulationObject;
		SimulationObject simulationObject3 = simulationObject.Children.Find((SimulationObject match) => match.Tags.Contains("ClassResearch"));
		float propertyValue = simulationObject3.GetPropertyValue("UnlockedTechnologyCount");
		simulationObject3 = simulationObject2.Children.Find((SimulationObject match) => match.Tags.Contains("ClassResearch"));
		float propertyValue2 = simulationObject3.GetPropertyValue("UnlockedTechnologyCount");
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
		return aiParameterValue * num3;
	}

	public float WantedPrestigePoint { get; private set; }

	public DecisionResult EvaluateDecision(DiplomaticTerm term, global::Empire otherEmpire, EvaluationData<DiplomaticTerm, InterpreterContext> evaluationData = null)
	{
		Diagnostics.Assert(this.diplomaticTermEvaluator != null && this.diplomaticTermEvaluator.Context != null);
		InterpreterContext context = this.diplomaticTermEvaluator.Context;
		context.Clear();
		this.empire.Refresh(true);
		otherEmpire.Refresh(true);
		DecisionResult result = this.diplomaticTermEvaluator.Evaluate(term, null);
		this.lastEmpireInInterpreter = null;
		return result;
	}

	private void EvaluateDecisions(IEnumerable<DiplomaticTerm> terms, global::Empire otherEmpire, ref List<DecisionResult> decisions)
	{
		Diagnostics.Assert(this.diplomaticTermEvaluator != null && this.diplomaticTermEvaluator.Context != null);
		InterpreterContext context = this.diplomaticTermEvaluator.Context;
		context.Clear();
		this.empire.Refresh(true);
		otherEmpire.Refresh(true);
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
		interpreterContext.Register("Provider", empireWhichProvides.SimulationObject);
		interpreterContext.Register("Receiver", empireWhichReceives.SimulationObject);
		interpreterContext.Register("MyEmpire", this.empire.SimulationObject);
		interpreterContext.Register("OtherEmpire", empire.SimulationObject);
		interpreterContext.Register("MyEmpireIndex", this.empire.Index);
		interpreterContext.SimulationObject = this.empire.SimulationObject;
		this.FillInterpreterContext(empire, interpreterContext);
		DiplomaticTermTechnologyExchange diplomaticTermTechnologyExchange = diplomaticTerm as DiplomaticTermTechnologyExchange;
		if (diplomaticTermTechnologyExchange != null)
		{
			DecisionResult decisionResult = this.aiLayerResearch.EvaluateTechnology(diplomaticTermTechnologyExchange.TechnologyDefinition);
			float num = (float)this.departmentOfScience.GetTechnologyRemainingTurn(diplomaticTermTechnologyExchange.TechnologyDefinition);
			interpreterContext.Register("TechnologyEra", (float)DepartmentOfScience.GetTechnologyEraNumber(diplomaticTermTechnologyExchange.TechnologyDefinition));
			interpreterContext.Register("TechnologyEvaluationScore", (decisionResult.FailureFlags != null && decisionResult.FailureFlags.Length != 0) ? 0f : decisionResult.Score);
			interpreterContext.Register("TechnologyRemainingTurn", num);
		}
		IWorldAtlasAIHelper service = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		float worldExplorationRatio = service.GetWorldExplorationRatio(this.empire);
		float worldExplorationRatio2 = service.GetWorldExplorationRatio(empire);
		float num2 = Mathf.Max(0.1f, worldExplorationRatio2) / Mathf.Max(0.1f, worldExplorationRatio);
		interpreterContext.Register("TheirExplorationlead", num2);
		float propertyValue = this.empire.GetPropertyValue("EmpireScaleFactor");
		float propertyValue2 = empire.GetPropertyValue("EmpireScaleFactor");
		float num3 = propertyValue2 / propertyValue;
		interpreterContext.Register("TheirScaleLead", num3);
		DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm as DiplomaticTermResourceExchange;
		if (diplomaticTermResourceExchange != null)
		{
			interpreterContext.Register("ResourceAmount", diplomaticTermResourceExchange.Amount);
			float priceWithSalesTaxes = TradableResource.GetPriceWithSalesTaxes(diplomaticTermResourceExchange.ResourceName, TradableTransactionType.Buyout, empireWhichReceives, 1f);
			interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes);
			DepartmentOfTheTreasury agency = empireWhichProvides.GetAgency<DepartmentOfTheTreasury>();
			float num4;
			agency.TryGetResourceStockValue(empireWhichProvides.SimulationObject, diplomaticTermResourceExchange.ResourceName, out num4, true);
			interpreterContext.Register("ProviderResourceStock", num4);
			DepartmentOfTheTreasury agency2 = empireWhichReceives.GetAgency<DepartmentOfTheTreasury>();
			float num5;
			agency2.TryGetResourceStockValue(empireWhichReceives.SimulationObject, diplomaticTermResourceExchange.ResourceName, out num5, true);
			interpreterContext.Register("ReceiverResourceStock", num5);
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
					float num6 = 0f;
					for (int i = 0; i < city.Region.Borders.Length; i++)
					{
						Region.Border border = city.Region.Borders[i];
						Region region = this.worldPositionningService.GetRegion(border.NeighbourRegionIndex);
						if (region.City != null && region.City.Empire != null && region.City.Empire.Index == city.Empire.Index)
						{
							num6 += (float)border.WorldPositions.Length;
						}
					}
					interpreterContext.Register("BorderLengthCommonWithMyEmpire", num6);
					interpreterContext.Register("IsCapital", (!city.SimulationObject.Tags.Contains(City.TagMainCity)) ? 0f : 1f);
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
			float num7 = (float)((diplomaticTermBoosterExchange.BoosterGUID == null) ? 0 : diplomaticTermBoosterExchange.BoosterGUID.Length);
			interpreterContext.Register("ResourceAmount", num7);
			Diagnostics.Assert(this.departmentOfPlanificationAndDevelopment != null);
			float num8 = (float)this.departmentOfPlanificationAndDevelopment.CountBoosters((BoosterDefinition match) => match.Name == diplomaticTermBoosterExchange.BoosterDefinitionName);
			interpreterContext.Register("BoosterCount", num8);
			float priceWithSalesTaxes2 = TradableBooster.GetPriceWithSalesTaxes(diplomaticTermBoosterExchange.BoosterDefinitionName, TradableTransactionType.Sellout, this.empire, num7);
			interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes2);
		}
		DiplomaticTermProposal diplomaticTermProposal = diplomaticTerm as DiplomaticTermProposal;
		if (diplomaticTermProposal != null)
		{
			interpreterContext.SimulationObject = this.empire;
			bool flag = diplomaticTerm.EmpireWhichReceives == this.empire;
			interpreterContext.Register("AskedByMe", (!flag) ? 1 : 0);
			interpreterContext.Register("ProposalScore", 0f);
			interpreterContext.Register("ThirdParty", diplomaticTermProposal.ChosenEmpire.SimulationObject);
			this.FillInterpreterContext(diplomaticTermProposal.ChosenEmpire, interpreterContext);
		}
		DiplomaticTermPrisonerExchange diplomaticTermPrisonerExchange = diplomaticTerm as DiplomaticTermPrisonerExchange;
		if (diplomaticTermPrisonerExchange != null)
		{
			interpreterContext.SimulationObject = this.empire;
			bool flag2 = diplomaticTerm.EmpireWhichReceives == this.empire;
			interpreterContext.Register("AskedByMe", (!flag2) ? 1 : 0);
			IGameEntity gameEntity2;
			if (this.gameEntityRepositoryService.TryGetValue(diplomaticTermPrisonerExchange.HeroGuid, out gameEntity2) && gameEntity2 is Unit)
			{
				Unit unit = gameEntity2 as Unit;
				interpreterContext.Register("Hero", unit);
				float priceWithSalesTaxes3 = TradableUnit.GetPriceWithSalesTaxes(unit, TradableTransactionType.Buyout, empireWhichReceives, 1f);
				interpreterContext.Register("MarketPlaceValue", priceWithSalesTaxes3);
			}
		}
		DiplomaticTermFortressExchange fortressExchange = diplomaticTerm as DiplomaticTermFortressExchange;
		if (fortressExchange != null)
		{
			global::Empire empire2 = this.game.Empires.First((global::Empire match) => match is NavalEmpire);
			if (empire2 == null)
			{
				return;
			}
			PirateCouncil agency3 = empire2.GetAgency<PirateCouncil>();
			if (agency3 == null)
			{
				return;
			}
			Fortress fortress = agency3.Fortresses.FirstOrDefault((Fortress match) => match.GUID == fortressExchange.FortressGUID);
			if (fortress == null)
			{
				return;
			}
			int num9 = 0;
			if (fortress.Region.Owner == empireWhichProvides)
			{
				num9 = 1;
			}
			int num10 = 0;
			int num11 = 0;
			int num12 = 0;
			for (int j = 0; j < agency3.Fortresses.Count; j++)
			{
				if (agency3.Fortresses[j].Region == fortress.Region)
				{
					num10++;
					if (agency3.Fortresses[j].Occupant == empireWhichProvides)
					{
						num11++;
					}
					if (agency3.Fortresses[j].Occupant == empireWhichReceives)
					{
						num12++;
					}
				}
			}
			interpreterContext.SimulationObject = fortress.SimulationObject;
			interpreterContext.Register("IsRegionControlled", num9);
			interpreterContext.Register("TotalNumberOfFortressesInRegion", num10);
			interpreterContext.Register("ProviderNumberOfFortressesInRegion", num11);
			interpreterContext.Register("ReceiverNumberOfFortressesInRegion", num12);
			float num13 = 0f;
			NavyRegionData navyRegionData = this.aiLayerNavy.NavyRegions.Find((BaseNavyRegionData match) => match.WaterRegionIndex == fortress.Region.Index) as NavyRegionData;
			if (navyRegionData != null)
			{
				num13 = navyRegionData.RegionScore;
			}
			interpreterContext.Register("MyRegionScore", num13);
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
					if (diplomaticTermDefinition != null)
					{
						if (!(diplomaticTermDefinition is DiplomaticTermDiplomaticRelationStateDefinition))
						{
							DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, this.empire, empire, ref this.diplomaticTermsCacheList);
							DepartmentOfForeignAffairs.GetDiplomaticTerms(diplomaticTermDefinition, diplomaticContract, empire, this.empire, ref this.diplomaticTermsCacheList);
						}
					}
				}
			}
		}
		Diagnostics.Assert(this.availableTermEvaluationsCacheList != null);
		this.availableTermEvaluationsCacheList.Clear();
		for (int j = 0; j < this.diplomaticTermsCacheList.Count; j++)
		{
			DiplomaticTerm diplomaticTerm = this.diplomaticTermsCacheList[j];
			AILayer_Diplomacy.TermEvaluation termEvaluation = new AILayer_Diplomacy.TermEvaluation(diplomaticTerm);
			DiplomaticTermResourceExchange diplomaticTermResourceExchange = diplomaticTerm as DiplomaticTermResourceExchange;
			if (diplomaticTermResourceExchange != null)
			{
				float num;
				float maximumAmount;
				this.GetDiplomaticTermAmountLimits(diplomaticTerm, out num, out maximumAmount);
				diplomaticTermResourceExchange.Amount = num;
				termEvaluation.MinimumAmount = num;
				termEvaluation.MaximumAmount = maximumAmount;
			}
			this.availableTermEvaluationsCacheList.Add(termEvaluation);
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
		this.availableTermEvaluationsCacheList.Sort((AILayer_Diplomacy.TermEvaluation left, AILayer_Diplomacy.TermEvaluation right) => left.MaximumAmountScore.CompareTo(right.MaximumAmountScore));
		float num2 = this.AnalyseContractProposition(diplomaticContract);
		Interval otherInterval = new Interval(this.minimumContractPropositionEvaluationScore, this.maximumContractPropositionEvaluationScore);
		Interval interval = new Interval(num2, num2);
		while (diplomaticTerms.Count < 6 && this.availableTermEvaluationsCacheList.Count > 0 && !interval.Intersect(otherInterval))
		{
			AILayer_Diplomacy.TermEvaluation termEvaluation3 = null;
			if (interval.IsGreaterThan(otherInterval))
			{
				for (int l = 0; l < this.availableTermEvaluationsCacheList.Count; l++)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation4 = this.availableTermEvaluationsCacheList[l];
					if (termEvaluation4.LowerScore <= 0f)
					{
						Interval interval2 = new Interval(interval.LowerBound + termEvaluation4.LowerScore, interval.UpperBound + termEvaluation4.GreaterScore);
						if (!interval2.IsLowerThan(otherInterval))
						{
							termEvaluation3 = termEvaluation4;
							break;
						}
					}
				}
			}
			else if (interval.IsLowerThan(otherInterval))
			{
				for (int m = this.availableTermEvaluationsCacheList.Count - 1; m >= 0; m--)
				{
					AILayer_Diplomacy.TermEvaluation termEvaluation5 = this.availableTermEvaluationsCacheList[m];
					if (termEvaluation5.GreaterScore >= 0f)
					{
						Interval interval3 = new Interval(interval.LowerBound + termEvaluation5.LowerScore, interval.UpperBound + termEvaluation5.GreaterScore);
						if (!interval3.IsGreaterThan(otherInterval))
						{
							termEvaluation3 = termEvaluation5;
							break;
						}
					}
				}
			}
			if (termEvaluation3 == null)
			{
				break;
			}
			diplomaticTerms.Add(termEvaluation3.Term);
			this.availableTermEvaluationsCacheList.Remove(termEvaluation3);
			interval = new Interval(interval.LowerBound + termEvaluation3.LowerScore, interval.UpperBound + termEvaluation3.GreaterScore);
		}
		if (interval.UpperBound > interval.LowerBound)
		{
			num2 = this.AnalyseContractProposition(diplomaticTerms, empire);
			if (num2 < this.minimumContractPropositionEvaluationScore || num2 > this.maximumContractPropositionEvaluationScore)
			{
				if (interval.UpperBound <= 0f)
				{
					for (int n = 0; n < diplomaticTerms.Count; n++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange2 = diplomaticTerms[n] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange2 != null)
						{
							float num3;
							float amount;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange2, out num3, out amount);
							diplomaticTermResourceExchange2.Amount = amount;
						}
					}
				}
				else if (interval.LowerBound >= 0f)
				{
					for (int num4 = 0; num4 < diplomaticTerms.Count; num4++)
					{
						DiplomaticTermResourceExchange diplomaticTermResourceExchange3 = diplomaticTerms[num4] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange3 != null)
						{
							float amount2;
							float num5;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange3, out amount2, out num5);
							diplomaticTermResourceExchange3.Amount = amount2;
						}
					}
				}
				else if (interval.UpperBound > 0f && interval.LowerBound < 0f)
				{
					int num6 = 0;
					float num7 = 0f;
					while (num2 < this.minimumContractPropositionEvaluationScore || num2 > this.maximumContractPropositionEvaluationScore)
					{
						if (num6 >= diplomaticTerms.Count)
						{
							break;
						}
						DiplomaticTermResourceExchange diplomaticTermResourceExchange4 = diplomaticTerms[num6] as DiplomaticTermResourceExchange;
						if (diplomaticTermResourceExchange4 == null)
						{
							num6++;
						}
						else
						{
							float num8;
							float num9;
							this.GetDiplomaticTermAmountLimits(diplomaticTermResourceExchange4, out num8, out num9);
							float num10 = num9 - num8;
							DecisionResult decisionResult = this.EvaluateDecision(diplomaticTermResourceExchange4, empire, null);
							if ((num2 < this.minimumContractPropositionEvaluationScore && decisionResult.Score > 0f) || (num2 > this.maximumContractPropositionEvaluationScore && decisionResult.Score < 0f))
							{
								if (num7 < 0f)
								{
									num6++;
									continue;
								}
								float num11 = Mathf.Max(0.1f * num10, 1f);
								float num12 = Mathf.Floor(diplomaticTermResourceExchange4.Amount + num11);
								if (num12 >= num9)
								{
									diplomaticTermResourceExchange4.Amount = num9;
									num6++;
									continue;
								}
								num7 = num11;
								diplomaticTermResourceExchange4.Amount = num12;
							}
							else if ((num2 < this.minimumContractPropositionEvaluationScore && decisionResult.Score < 0f) || (num2 > this.maximumContractPropositionEvaluationScore && decisionResult.Score > 0f))
							{
								if (num7 > 0f)
								{
									num6++;
									continue;
								}
								float num13 = Mathf.Max(0.1f * num10, 1f);
								float num14 = Mathf.Floor(diplomaticTermResourceExchange4.Amount - num13);
								if (num14 <= num8)
								{
									diplomaticTermResourceExchange4.Amount = num8;
									num6++;
									continue;
								}
								num7 = -num13;
								diplomaticTermResourceExchange4.Amount = num14;
							}
							num2 = this.AnalyseContractProposition(diplomaticTerms, empire);
						}
					}
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
			if (AILayer_Diplomacy.<>f__switch$map3 == null)
			{
				AILayer_Diplomacy.<>f__switch$map3 = new Dictionary<string, int>(6)
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
			if (AILayer_Diplomacy.<>f__switch$map3.TryGetValue(text, out num7))
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
		IGameService gameService = Services.GetService<IGameService>();
		Diagnostics.Assert(gameService != null);
		this.tickableRepositoryAIHelper = AIScheduler.Services.GetService<ITickableRepositoryAIHelper>();
		this.tickableRepositoryAIHelper.Register(this);
		this.game = (gameService.Game as global::Game);
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
		this.departmentOfForeignAffairs.DiplomaticRelationStateChange += this.DepartmentOfForeignAffairs_DiplomaticRelationStateChange;
		this.diplomacyService = this.game.Services.GetService<IDiplomacyService>();
		this.diplomacyContractRepositoryService = this.game.Services.GetService<IDiplomaticContractRepositoryService>();
		this.gameEntityRepositoryService = this.game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.worldPositionningService = this.game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
		Diagnostics.Assert(aiEntity.AIPlayer != null);
		AIEntity aiEntityAmas = aiEntity.AIPlayer.GetEntity<AIEntity_Amas>();
		Diagnostics.Assert(aiEntityAmas != null);
		this.aiLayerDiplomacyAmas = aiEntityAmas.GetLayer<AILayer_DiplomacyAmas>();
		this.aiLayerResourceAmas = aiEntityAmas.GetLayer<AILayer_ResourceAmas>();
		Diagnostics.Assert(this.aiLayerDiplomacyAmas != null);
		this.aiLayerAccountManager = aiEntity.GetLayer<AILayer_AccountManager>();
		Diagnostics.Assert(this.aiLayerAccountManager != null);
		this.aiLayerResearch = base.AIEntity.GetLayer<AILayer_Research>();
		Diagnostics.Assert(this.aiLayerResearch != null);
		this.aiLayerAttitude = aiEntity.GetLayer<AILayer_Attitude>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		this.aiLayerNavy = aiEntity.GetLayer<AILayer_Navy>();
		Diagnostics.Assert(this.aiLayerAttitude != null);
		DepartmentOfForeignAffairs.ConstructibleElement contructibleElement;
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.Warning, out contructibleElement))
		{
			this.diplomaticTermWarning = (contructibleElement as DiplomaticTermDefinition);
		}
		if (this.contructibleElementDatabase.TryGetValue(DiplomaticTermDefinition.Names.Gratify, out contructibleElement))
		{
			this.diplomaticTermGratify = (contructibleElement as DiplomaticTermDefinition);
		}
		Diagnostics.Assert(this.diplomaticTermWarning != null);
		Diagnostics.Assert(this.diplomaticTermGratify != null);
		this.InitializeData();
		this.InitializeDiplomaticTermEvaluationHelper();
		this.InitializeWishes();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_Diplomacy_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_Diplomacy_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Diplomacy_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
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
			this.tickableRepositoryAIHelper = null;
		}
		this.empire = null;
		this.departmentOfScience = null;
		this.departmentOfPlanificationAndDevelopment = null;
		this.departmentOfTheInterior = null;
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
		this.diplomaticRelationStateByAmasAgentName.Clear();
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		int count = this.departmentOfTheInterior.Cities.Count;
		int currentTechnologyEraNumber = this.departmentOfScience.CurrentTechnologyEraNumber;
		float value = (float)(count * currentTechnologyEraNumber) * this.diplomaticMaximumAccountMultiplier;
		AILayer_AccountManager layer = base.AIEntity.GetLayer<AILayer_AccountManager>();
		layer.SetMaximalAccount(AILayer_AccountManager.DiplomacyAccountName, value);
		this.ClearContractRequests();
		Blackboard blackboard = base.AIEntity.AIPlayer.Blackboard;
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			if (majorEmpire.Index != this.empire.Index)
			{
				DiplomaticRelation diplomaticRelation = this.departmentOfForeignAffairs.GetDiplomaticRelation(majorEmpire);
				Diagnostics.Assert(diplomaticRelation != null);
				if (diplomaticRelation.State != null && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown) && !(diplomaticRelation.State.Name == DiplomaticRelationState.Names.Dead))
				{
					if (blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == majorEmpire.Index) == null)
					{
						float criticity;
						DiplomaticRelationState wantedDiplomaticRelationStateWith = this.GetWantedDiplomaticRelationStateWith(majorEmpire, out criticity);
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
		for (int i = 0; i < this.majorEmpires.Length; i++)
		{
			MajorEmpire majorEmpire = this.majorEmpires[i];
			Diagnostics.Assert(majorEmpire != null);
			if (majorEmpire.Index != this.empire.Index)
			{
				this.GenerateContractRequests(majorEmpire);
			}
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.currentContractRequest = null;
		if (this.ContractRequests.Count > 0)
		{
			this.ContractRequests[0].State = AILayer_Diplomacy.ContractRequest.ContractRequestState.WaitingToBeProcessed;
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
		}
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

	private const bool DisableForceStatus = false;

	public const string AIDiplomacyRegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_Diplomacy/";

	private List<DecisionResult> decisions;

	private IDatabase<AIParameterConverter> aiParameterConverterDatabase;

	private Dictionary<StaticString, IAIParameter<InterpreterContext>[]> aiParametersByElement = new Dictionary<StaticString, IAIParameter<InterpreterContext>[]>();

	public List<AILayer_Diplomacy.ContractRequest> ContractRequests = new List<AILayer_Diplomacy.ContractRequest>();

	[InfluencedByPersonality]
	private int minimumNumberOfTurnsBetweenPropositions = 5;

	[InfluencedByPersonality]
	private int maximumNumberOfTurnsBetweenPropositions = 10;

	[InfluencedByPersonality]
	private int maximumNumberOfTurnsBetweenStatusAndForceStatus = 20;

	private AILayer_Diplomacy.ContractRequest currentContractRequest;

	public FixedSizedList<EvaluationData<DiplomaticTerm, InterpreterContext>> DebugEvaluationsHistoric = new FixedSizedList<EvaluationData<DiplomaticTerm, InterpreterContext>>(global::Application.FantasyPreferences.AIDebugHistoricSize);

	private ElementEvaluator<DiplomaticTerm, InterpreterContext> diplomaticTermEvaluator;

	private AILayer_Research aiLayerResearch;

	private List<AILayer_Diplomacy.TermEvaluation> availableTermEvaluationsCacheList = new List<AILayer_Diplomacy.TermEvaluation>();

	private List<DiplomaticTerm> diplomaticTermsCacheList = new List<DiplomaticTerm>();

	private global::Empire lastEmpireInInterpreter;

	[InfluencedByPersonality]
	private float diplomaticRelationStateAgentCriticityThreshold = 0.5f;

	[InfluencedByPersonality]
	private float diplomaticTermAgentCriticityThreshold = 0.5f;

	[InfluencedByPersonality]
	private float diplomaticRelationStateAgentCriticityEpsilon = 0.05f;

	[InfluencedByPersonality]
	private float maximumContractPropositionEvaluationScore = 10f;

	[InfluencedByPersonality]
	private float minimumContractPropositionEvaluationScore;

	[InfluencedByPersonality]
	private float maximumExchangeAmountRatio = 0.8f;

	[InfluencedByPersonality]
	private float minimumExchangeAmountRatio = 0.2f;

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

	private float diplomaticMaximumAccountMultiplier = 25f;

	private Dictionary<StaticString, DiplomaticRelationState> diplomaticRelationStateByAmasAgentName = new Dictionary<StaticString, DiplomaticRelationState>();

	private IDatabase<DiplomaticRelationState> diplomaticRelationStateDatabase;

	private Dictionary<StaticString, DiplomaticTermDefinition[]> diplomaticTermByAmasAgentName = new Dictionary<StaticString, DiplomaticTermDefinition[]>();

	private DiplomaticTermDefinition diplomaticTermGratify;

	private DiplomaticTermDefinition diplomaticTermWarning;

	private global::Empire empire;

	private global::Game game;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private MajorEmpire[] majorEmpires;

	private StaticString[] mostWantedDiplomaticTermAgentNameByEmpireIndex;

	private Dictionary<StaticString, DiplomaticTermDefinition[]> proposalTermByAmasAgentName = new Dictionary<StaticString, DiplomaticTermDefinition[]>();

	private ITickableRepositoryAIHelper tickableRepositoryAIHelper;

	private IWorldPositionningService worldPositionningService;

	private IEventService eventService;

	[InfluencedByPersonality]
	private int wantedDiplomaticRelationStateMessageRevaluationPeriod = 4;

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
