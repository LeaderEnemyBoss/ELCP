using System;
using System.Collections.Generic;
using Amplitude;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_ResourceManager/CityBooster", new object[]
{

})]
public class AIBoosterManager_CityResources : AIBoosterManager
{
	protected internal override void CreateLocals()
	{
		base.CreateLocals();
		this.availableBoosterByDefinitionName.Clear();
		for (int i = 0; i < this.departmentOfEducation.VaultCount; i++)
		{
			BoosterDefinition boosterDefinition = this.departmentOfEducation.VaultItems[i].Constructible as BoosterDefinition;
			if (boosterDefinition != null)
			{
				StaticString name = boosterDefinition.Name;
				if (!this.availableBoosterByDefinitionName.ContainsKey(name))
				{
					this.availableBoosterByDefinitionName.Add(name, new List<GameEntityGUID>());
				}
				this.availableBoosterByDefinitionName[name].Add(this.departmentOfEducation.VaultItems[i].GUID);
			}
		}
	}

	protected internal override void Evaluate()
	{
		base.Evaluate();
		this.boosterNeedsMessages.Clear();
		this.boosterNeedsMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<CityBoosterNeeds>(BlackboardLayerID.Empire));
		for (int i = 0; i < this.boosterNeedsMessages.Count; i++)
		{
			if (this.boosterNeedsMessages[i].BoosterGuid.IsValid)
			{
				VaultItem vaultItem = this.departmentOfEducation[this.boosterNeedsMessages[i].BoosterGuid];
				if (vaultItem != null)
				{
					StaticString staticString = vaultItem.Constructible.Name;
					if (staticString == AIBoosterManager_CityResources.boosterCadavers)
					{
						staticString = AIBoosterManager_CityResources.boosterFood;
					}
					if (this.availableBoosterByDefinitionName.ContainsKey(staticString))
					{
						this.availableBoosterByDefinitionName[staticString].Remove(vaultItem.GUID);
						if (this.availableBoosterByDefinitionName[staticString].Count == 0)
						{
							this.availableBoosterByDefinitionName.Remove(staticString);
						}
					}
					this.boosterNeedsMessages[i].AvailabilityState = CityBoosterNeeds.CityBoosterState.Available;
				}
			}
		}
		this.boosterNeedsMessages.RemoveAll((CityBoosterNeeds match) => match.AvailabilityState != CityBoosterNeeds.CityBoosterState.Pending);
		this.boosterNeedsMessages.Sort((CityBoosterNeeds left, CityBoosterNeeds right) => -1 * left.BoosterPriority.CompareTo(right.BoosterPriority));
		for (int j = 0; j < this.boosterNeedsMessages.Count; j++)
		{
			CityBoosterNeeds cityBoosterNeeds = this.boosterNeedsMessages[j];
			StaticString staticString = cityBoosterNeeds.BoosterDefinitionName;
			if (this.availableBoosterByDefinitionName.ContainsKey(cityBoosterNeeds.BoosterDefinitionName))
			{
				int num = this.availableBoosterByDefinitionName[cityBoosterNeeds.BoosterDefinitionName].Count - 1;
				GameEntityGUID boosterGuid = this.availableBoosterByDefinitionName[cityBoosterNeeds.BoosterDefinitionName][num];
				this.availableBoosterByDefinitionName[cityBoosterNeeds.BoosterDefinitionName].RemoveAt(num);
				if (num == 0)
				{
					this.availableBoosterByDefinitionName.Remove(cityBoosterNeeds.BoosterDefinitionName);
				}
				cityBoosterNeeds.BoosterGuid = boosterGuid;
				cityBoosterNeeds.AvailabilityState = CityBoosterNeeds.CityBoosterState.Available;
				this.boosterNeedsMessages.RemoveAt(j);
				j--;
			}
		}
		this.evaluableMessages.Clear();
		this.evaluableMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage_CityBooster>(BlackboardLayerID.Empire));
		this.evaluableMessages.RemoveAll((EvaluableMessage_CityBooster match) => match.State != BlackboardMessage.StateValue.Message_InProgress || match.EvaluationState != EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining);
		this.evaluableMessages.Sort((EvaluableMessage_CityBooster left, EvaluableMessage_CityBooster right) => -1 * left.Interest.CompareTo(right.Interest));
		for (int k = 0; k < this.evaluableMessages.Count; k++)
		{
			EvaluableMessage_CityBooster cityBoosterEvaluableMessage = this.evaluableMessages[k];
			int num2 = this.boosterNeedsMessages.FindIndex((CityBoosterNeeds match) => match.BoosterDefinitionName == cityBoosterEvaluableMessage.BoosterDefinitionName);
			if (num2 >= 0)
			{
				CityBoosterNeeds cityBoosterNeeds2 = this.boosterNeedsMessages[num2];
				cityBoosterEvaluableMessage.Refresh(this.globalPriority, cityBoosterNeeds2.BoosterPriority, cityBoosterNeeds2.CityGuid);
				this.boosterNeedsMessages.RemoveAt(num2);
			}
			else if (cityBoosterEvaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining)
			{
				cityBoosterEvaluableMessage.Refresh(0f, 0f, cityBoosterEvaluableMessage.CityGuid);
			}
			else
			{
				base.AIEntity.AIPlayer.Blackboard.CancelMessage(this.evaluableMessages[k]);
				this.evaluableMessages.RemoveAt(k);
				k--;
			}
		}
		for (int l = 0; l < this.boosterNeedsMessages.Count; l++)
		{
			CityBoosterNeeds cityBoosterNeeds3 = this.boosterNeedsMessages[l];
			BoosterGeneratorDefinition boosterGenerator = this.constructibleElementHelper.GetBoosterGenerator(base.AIEntity.Empire, cityBoosterNeeds3.BoosterDefinitionName);
			if (boosterGenerator != null)
			{
				EvaluableMessage_CityBooster evaluableMessage_CityBooster = new EvaluableMessage_CityBooster(cityBoosterNeeds3.CityGuid, cityBoosterNeeds3.BoosterDefinitionName, boosterGenerator.Name, 1, AILayer_AccountManager.EconomyAccountName);
				evaluableMessage_CityBooster.SetInterest(this.globalPriority, cityBoosterNeeds3.BoosterPriority);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_CityBooster);
			}
		}
	}

	protected internal override void Execute()
	{
		base.Execute();
		this.synchronousJobRepository.RegisterSynchronousJob(new SynchronousJob(this.SynchronousJob_StartBooster));
	}

	protected internal override void Initialize(AIEntity aiEntity)
	{
		base.Initialize(aiEntity);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.Empire, this);
		this.departmentOfEducation = base.Empire.GetAgency<DepartmentOfEducation>();
		this.constructibleElementHelper = AIScheduler.Services.GetService<IConstructibleElementAIHelper>();
		this.synchronousJobRepository = AIScheduler.Services.GetService<ISynchronousJobRepositoryAIHelper>();
	}

	protected internal override void Release()
	{
		base.Release();
		this.departmentOfEducation = null;
		this.availableBoosterByDefinitionName.Clear();
		this.evaluableMessages.Clear();
		this.boosterNeedsMessages.Clear();
		this.constructibleElementHelper = null;
	}

	private void BuyoutAndActivateBooster_TicketRaised(object sender, TicketRaisedEventArgs e)
	{
	}

	private void StartNextBooster()
	{
		for (int i = 0; i < this.departmentOfEducation.VaultCount; i++)
		{
			BoosterDefinition boosterDefinition = this.departmentOfEducation.VaultItems[i].Constructible as BoosterDefinition;
			if (boosterDefinition != null && boosterDefinition.Name == "BoosterScience")
			{
				OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(base.Empire.Index, boosterDefinition.Name, this.departmentOfEducation.VaultItems[i].GUID, false);
				Ticket ticket;
				base.Empire.PlayerControllers.AI.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.BuyoutAndActivateBooster_TicketRaised));
			}
		}
	}

	private SynchronousJobState SynchronousJob_StartBooster()
	{
		this.StartNextBooster();
		return SynchronousJobState.Success;
	}

	private static StaticString boosterCadavers = "BoosterCadavers";

	private static StaticString boosterFood = "BoosterFood";

	private Dictionary<StaticString, List<GameEntityGUID>> availableBoosterByDefinitionName = new Dictionary<StaticString, List<GameEntityGUID>>();

	private List<CityBoosterNeeds> boosterNeedsMessages = new List<CityBoosterNeeds>();

	private IConstructibleElementAIHelper constructibleElementHelper;

	private DepartmentOfEducation departmentOfEducation;

	private List<EvaluableMessage_CityBooster> evaluableMessages = new List<EvaluableMessage_CityBooster>();

	private ISynchronousJobRepositoryAIHelper synchronousJobRepository;

	[InfluencedByPersonality]
	private float globalPriority = 0.5f;
}
