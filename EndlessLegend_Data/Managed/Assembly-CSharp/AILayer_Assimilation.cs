using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;

public class AILayer_Assimilation : AILayer
{
	protected Empire Empire
	{
		get
		{
			if (base.AIEntity == null)
			{
				return null;
			}
			return base.AIEntity.Empire;
		}
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.departmentOfTheInterior = this.Empire.GetAgency<DepartmentOfTheInterior>();
		Diagnostics.Assert(this.departmentOfTheInterior != null);
		this.departmentOfTheTreasury = this.Empire.GetAgency<DepartmentOfTheTreasury>();
		Diagnostics.Assert(this.departmentOfTheTreasury != null);
		this.unitBodyDefinitionDatabase = Databases.GetDatabase<UnitBodyDefinition>(false);
		Diagnostics.Assert(this.unitBodyDefinitionDatabase != null);
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		Diagnostics.Assert(this.intelligenceAIHelper != null);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILAyer_Assimilation_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILAyer_Assimilation_EvaluateNeedsPass", new AIEntity.AIAction(this.EvaluateNeeds), this, new StaticString[0]);
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILAyer_Assimilation_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		this.departmentOfTheInterior = null;
		this.departmentOfTheTreasury = null;
		this.unitBodyDefinitionDatabase = null;
		this.intelligenceAIHelper = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		float propertyValue = this.Empire.GetPropertyValue(SimulationProperties.MinorFactionSlotCount);
		int count = this.departmentOfTheInterior.AssimilatedFactions.Count;
		if (propertyValue <= (float)count)
		{
			return;
		}
		List<MinorFaction> list = new List<MinorFaction>();
		this.departmentOfTheInterior.GetAssimilableMinorFactions(ref list);
		if (list.Count == 0)
		{
			return;
		}
		int num = -1;
		float num2 = 0f;
		UnitBodyDefinition[] values = this.unitBodyDefinitionDatabase.GetValues();
		for (int i = 0; i < list.Count; i++)
		{
			Faction faction = list[i];
			if ((!this.Empire.SimulationObject.Tags.Contains("FactionTraitBuyOutPopulation") || !(faction.Name == "Bos")) && (!this.Empire.SimulationObject.Tags.Contains(FactionTrait.FactionTraitReplicants1) || !(faction.Name == "Haunts")) && !this.departmentOfTheInterior.IsAssimilated(faction))
			{
				foreach (UnitBodyDefinition unitBodyDefinition in from match in values
				where match.Affinity != null && match.Affinity.Name == faction.Affinity.Name
				select match)
				{
					float aistrengthBelief = this.intelligenceAIHelper.GetAIStrengthBelief(base.AIEntity.Empire.Index, unitBodyDefinition.Name);
					if (num < 0 || aistrengthBelief > num2)
					{
						num = i;
						num2 = aistrengthBelief;
					}
				}
			}
		}
		if (num < 0)
		{
			return;
		}
		Faction faction2 = list[num];
		EvaluableMessage_Assimilation evaluableMessage_Assimilation = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
		if (evaluableMessage_Assimilation == null)
		{
			evaluableMessage_Assimilation = new EvaluableMessage_Assimilation(AILayer_AccountManager.AssimilationAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_Assimilation);
		}
		evaluableMessage_Assimilation.Refresh(1f, 1f, faction2.Name);
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
		IEnumerable<EvaluableMessage_Assimilation> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
		foreach (EvaluableMessage_Assimilation evaluableMessage_Assimilation in messages)
		{
			evaluableMessage_Assimilation.UpdateBuyEvaluation("Assimilation", 0UL, DepartmentOfTheInterior.GetAssimilationCost(this.Empire, 0), 2, 0f, 0UL);
		}
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		IEnumerable<EvaluableMessage_Assimilation> messages = base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_Assimilation>(BlackboardLayerID.Empire, (EvaluableMessage_Assimilation match) => match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate && match.ChosenBuyEvaluation != null);
		using (IEnumerator<EvaluableMessage_Assimilation> enumerator = messages.GetEnumerator())
		{
			if (enumerator.MoveNext())
			{
				EvaluableMessage_Assimilation evaluableMessage_Assimilation = enumerator.Current;
				this.validatedAssimilationMessage = evaluableMessage_Assimilation;
				ISynchronousJobRepositoryAIHelper service = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
				service.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_Assimilate));
			}
		}
	}

	private void AssimilationOrder_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
		OrderAssimilateFaction orderAssimilateFaction = e.Order as OrderAssimilateFaction;
		if (this.validatedAssimilationMessage != null && orderAssimilateFaction.Instructions.Length > 0 && this.validatedAssimilationMessage.MinorFactionName == orderAssimilateFaction.Instructions[0].FactionName)
		{
			if (e.Result != PostOrderResponse.Processed)
			{
				this.validatedAssimilationMessage.SetFailedToObtain();
			}
			else
			{
				this.validatedAssimilationMessage.SetObtained();
			}
		}
	}

	private SynchronousJobState SynchronousJob_Assimilate()
	{
		if (this.validatedAssimilationMessage != null)
		{
			AILayer.Log("[AILayer_Assimilation] {0}: Sending assimilation order for faction {1}", new object[]
			{
				this.Empire.Name,
				this.validatedAssimilationMessage.MinorFactionName
			});
			OrderAssimilateFaction order = new OrderAssimilateFaction(this.Empire.Index, this.validatedAssimilationMessage.MinorFactionName, true);
			Ticket ticket;
			base.AIEntity.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.AssimilationOrder_TicketRaised));
			return SynchronousJobState.Success;
		}
		return SynchronousJobState.Failure;
	}

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IDatabase<UnitBodyDefinition> unitBodyDefinitionDatabase;

	private EvaluableMessage_Assimilation validatedAssimilationMessage;
}
