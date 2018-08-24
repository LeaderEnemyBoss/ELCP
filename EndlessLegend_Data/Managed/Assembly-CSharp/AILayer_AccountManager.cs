using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_AccountManager", new object[]
{

})]
public class AILayer_AccountManager : AILayer, IXmlSerializable
{
	public override void ReadXml(XmlReader reader)
	{
		int num = reader.ReadVersionAttribute();
		base.ReadXml(reader);
		this.accounts = new List<Account>();
		if (reader.IsStartElement("Accounts"))
		{
			int attribute = reader.GetAttribute<int>("Count");
			reader.ReadStartElement("Accounts");
			for (int i = 0; i < attribute; i++)
			{
				StaticString accountTag = reader.GetAttribute<string>("AccountTag");
				Account account = new Account(accountTag, 0f);
				account.CurrentProfitPercent = reader.GetAttribute<float>("CurrentProfitPercent");
				account.EstimatedBalance = reader.GetAttribute<float>("EstimatedBalance");
				account.PromisedAmount = reader.GetAttribute<float>("PromisedAmount");
				this.accounts.Add(account);
				reader.Skip("Account");
			}
			reader.ReadEndElement("Accounts");
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		int num = writer.WriteVersionAttribute(4);
		base.WriteXml(writer);
		if (num >= 4)
		{
			writer.WriteStartElement("Accounts");
			if (this.accounts != null)
			{
				writer.WriteAttributeString<int>("Count", this.accounts.Count);
				for (int i = 0; i < this.accounts.Count; i++)
				{
					Account account = this.accounts[i];
					writer.WriteStartElement("Account");
					writer.WriteAttributeString<string>("AccountTag", account.AccountTag);
					writer.WriteAttributeString<float>("CurrentProfitPercent", account.CurrentProfitPercent);
					writer.WriteAttributeString<float>("EstimatedBalance", account.EstimatedBalance);
					writer.WriteAttributeString<float>("PromisedAmount", account.PromisedAmount);
					writer.WriteEndElement();
				}
			}
			else
			{
				writer.WriteAttributeString<int>("Count", 0);
			}
			writer.WriteEndElement();
		}
	}

	public IEnumerable<Account> Debug_Accounts
	{
		get
		{
			return this.accounts;
		}
	}

	public StaticString GetAccountTagResource(StaticString leftTag)
	{
		return this.ResourceFromAccountTag(leftTag);
	}

	public int CompareAccountTags(StaticString leftTag, StaticString rightTag)
	{
		if (leftTag == rightTag)
		{
			return 0;
		}
		StaticString staticString = this.ResourceFromAccountTag(leftTag);
		StaticString obj = this.ResourceFromAccountTag(rightTag);
		return staticString.CompareHandleTo(obj);
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_AccountManager_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"AILayer_Economy_CreateLocalNeeds_ParseEvaluableMessage"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_AccountManager_ExecuteNeedsPass", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.AIEntity.Empire, this);
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Load()
	{
		yield return base.Load();
		if (this.accounts == null)
		{
			this.accounts = new List<Account>();
		}
		this.aiLayerEconomy = base.AIEntity.GetLayer<AILayer_Economy>();
		int militaryAccountIndex = this.EnsureAccount(AILayer_AccountManager.MilitaryAccountName);
		int economyAccountIndex = this.EnsureAccount(AILayer_AccountManager.EconomyAccountName);
		int heroAccountIndex = this.EnsureAccount(AILayer_AccountManager.HeroAccountName);
		int diplomacyAccountIndex = this.EnsureAccount(AILayer_AccountManager.DiplomacyAccountName);
		int empireplanAccountIndex = this.EnsureAccount(AILayer_AccountManager.EmpirePlanAccountName);
		int orbAccountIndex = this.EnsureAccount(AILayer_AccountManager.OrbAccountName);
		this.accountIndexByResource.Add(DepartmentOfTheTreasury.Resources.EmpirePoint, new int[]
		{
			diplomacyAccountIndex,
			empireplanAccountIndex
		});
		this.accountIndexByResource.Add(DepartmentOfTheTreasury.Resources.EmpireMoney, new int[]
		{
			militaryAccountIndex,
			economyAccountIndex,
			heroAccountIndex
		});
		this.accountIndexByResource.Add(DepartmentOfTheTreasury.Resources.Orb, new int[]
		{
			orbAccountIndex
		});
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.aiLayerEconomy = null;
		this.accounts.Clear();
		this.workingMessages.Clear();
		this.accountIndexByResource.Clear();
	}

	public void SetMaximalAccount(StaticString accountName, float value)
	{
		for (int i = 0; i < this.accounts.Count; i++)
		{
			if (this.accounts[i].AccountTag == accountName)
			{
				this.accounts[i].MaxAccount = value;
			}
		}
	}

	public Account TryGetAccount(StaticString accountName)
	{
		for (int i = 0; i < this.accounts.Count; i++)
		{
			if (this.accounts[i].AccountTag == accountName)
			{
				return this.accounts[i];
			}
		}
		return null;
	}

	public bool TryGetAccountInfos(StaticString accountName, StaticString ressourceName, out float credit, out float profitPercent)
	{
		profitPercent = 0f;
		credit = 0f;
		for (int i = 0; i < this.accounts.Count; i++)
		{
			if (this.accounts[i].AccountTag == accountName)
			{
				profitPercent = this.accounts[i].CurrentProfitPercent;
				credit = this.accounts[i].GetAvailableAmount();
				return true;
			}
		}
		return false;
	}

	public bool TryMakeUnexpectedImmediateExpense(StaticString accountName, float expense, float expensePriority)
	{
		int i = 0;
		while (i < this.accounts.Count)
		{
			Account account = this.accounts[i];
			if (account.AccountTag == accountName)
			{
				float num;
				if (expensePriority > account.WantedAmountPriority)
				{
					num = account.GetAvailableAmountWithoutPromise();
				}
				else
				{
					num = account.GetAvailableAmount();
				}
				if (num >= expense)
				{
					account.EstimatedBalance -= expense;
					return true;
				}
				return false;
			}
			else
			{
				i++;
			}
		}
		return false;
	}

	public float GetAvailableAmount(StaticString accountName, float expensePriority)
	{
		for (int i = 0; i < this.accounts.Count; i++)
		{
			Account account = this.accounts[i];
			if (account.AccountTag == accountName)
			{
				float result;
				if (expensePriority > account.WantedAmountPriority)
				{
					result = account.GetAvailableAmountWithoutPromise();
				}
				else
				{
					result = account.GetAvailableAmount();
				}
				return result;
			}
		}
		return 0f;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		AILayer_Strategy layer = base.AIEntity.GetLayer<AILayer_Strategy>();
		for (int i = 0; i < this.accounts.Count; i++)
		{
			this.accounts[i].CurrentProfitPercent = layer.GetScore(AILayer_Strategy.AccountManagerParameterModifier, this.accounts[i].AccountTag);
		}
		int[] array = this.accountIndexByResource[DepartmentOfTheTreasury.Resources.EmpirePoint];
		for (int j = 0; j < array.Length; j++)
		{
			this.ApplyExecutedMessages(this.accounts[array[j]]);
		}
		this.UpdateAccountBalance(DepartmentOfTheTreasury.Resources.EmpirePoint, array);
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.ValidateAccountMessagesByResource(DepartmentOfTheTreasury.Resources.EmpireMoney);
		this.ValidateAccountMessagesByResource(DepartmentOfTheTreasury.Resources.Orb);
		int[] array = this.accountIndexByResource[DepartmentOfTheTreasury.Resources.EmpirePoint];
		for (int i = 0; i < array.Length; i++)
		{
			this.ValidateAccountMessages(this.accounts[array[i]]);
		}
	}

	private void ApplyExecutedMessages(Account account)
	{
		this.workingMessages.Clear();
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.City));
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.Empire));
		this.workingMessages.RemoveAll((EvaluableMessage match) => match.AccountTag != account.AccountTag || match.ChosenBuyEvaluation == null);
		for (int i = 0; i < this.workingMessages.Count; i++)
		{
			EvaluableMessage evaluableMessage = this.workingMessages[i];
			if (evaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel || evaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage.ChosenBuyEvaluation.State != BuyEvaluation.EvaluationState.Purchased)
			{
				account.PromisedAmount -= evaluableMessage.ChosenBuyEvaluation.DustCost;
				if (account.PromisedAmount < 0f)
				{
					account.PromisedAmount = 0f;
				}
				evaluableMessage.RevokeChosenBuyEvaluation();
			}
			else if (evaluableMessage.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtained && evaluableMessage.ChosenBuyEvaluation.State == BuyEvaluation.EvaluationState.Purchased)
			{
				account.PromisedAmount -= evaluableMessage.ChosenBuyEvaluation.DustCost;
				if (account.PromisedAmount < 0f)
				{
					account.PromisedAmount = 0f;
				}
				account.EstimatedBalance -= evaluableMessage.ChosenBuyEvaluation.DustCost;
				if (account.EstimatedBalance < 0f)
				{
					account.EstimatedBalance = 0f;
				}
			}
		}
		if (account.PromisedAmount > 0f && this.workingMessages.Count == 0)
		{
			account.PromisedAmount = 0f;
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				Diagnostics.Log("ELCP: Empire {0} ApplyExecutedMessages workaround fix, resetting PromisedAmount of {1} to 0.", new object[]
				{
					base.AIEntity.Empire.Index,
					account.AccountTag
				});
			}
		}
	}

	private int EnsureAccount(StaticString accountTag)
	{
		Account account = this.accounts.Find((Account match) => match.AccountTag == accountTag);
		if (account == null)
		{
			account = new Account(accountTag, 0f);
			this.accounts.Add(account);
		}
		return this.accounts.IndexOf(account);
	}

	private void UpdateAccountBalance(StaticString resourceName, int[] accountIndexForResource)
	{
		DepartmentOfTheTreasury agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		float estimatedNetOutcome = 0f;
		agency.TryGetNetResourceValue(base.AIEntity.Empire, resourceName, out estimatedNetOutcome, false);
		float num2 = 0f;
		for (int i = 0; i < accountIndexForResource.Length; i++)
		{
			Account account = this.accounts[accountIndexForResource[i]];
			account.EstimatedNetOutcome = estimatedNetOutcome;
			if (account.MaxAccount >= 0f && account.MaxAccount <= account.EstimatedBalance)
			{
				account.EstimatedBalance = account.MaxAccount;
				num += account.MaxAccount;
			}
			else
			{
				num += account.EstimatedBalance;
				num2 += account.CurrentProfitPercent;
			}
		}
		float num3 = 0f;
		if (num2 == 0f || !agency.TryGetResourceStockValue(base.AIEntity.Empire, resourceName, out num3, false) || num3 == 0f)
		{
			num3 = 0f;
			for (int j = 0; j < accountIndexForResource.Length; j++)
			{
				this.accounts[accountIndexForResource[j]].EstimatedBalance = 0f;
			}
			return;
		}
		float num4 = num3;
		num4 -= num;
		if (Mathf.Abs(num4) <= (float)accountIndexForResource.Length)
		{
			return;
		}
		if (num4 < 0f)
		{
			for (int k = 0; k < accountIndexForResource.Length; k++)
			{
				Account account2 = this.accounts[accountIndexForResource[k]];
				if (account2.CurrentProfitPercent < 0.01f)
				{
					num4 += account2.EstimatedBalance;
					account2.EstimatedBalance = 0f;
					if (num4 >= 0f)
					{
						account2.EstimatedBalance = num4;
						break;
					}
				}
			}
		}
		int num5 = 0;
		while (Mathf.Abs(num4) > 0.1f && num5 < 100)
		{
			num5++;
			float num6 = 0f;
			for (int l = 0; l < accountIndexForResource.Length; l++)
			{
				Account account3 = this.accounts[accountIndexForResource[l]];
				if (num4 <= 0f || account3.MaxAccount < 0f || account3.MaxAccount > account3.EstimatedBalance)
				{
					int index = accountIndexForResource[l];
					float num7 = num4 * (account3.CurrentProfitPercent / num2);
					account3.EstimatedBalance += num7;
					num6 += num7;
					if (account3.EstimatedBalance < 0f)
					{
						num6 -= this.accounts[index].EstimatedBalance;
						account3.EstimatedBalance = 0f;
					}
					if (account3.MaxAccount >= 0f && account3.MaxAccount < account3.EstimatedBalance)
					{
						num6 += account3.EstimatedBalance - account3.MaxAccount;
						account3.EstimatedBalance = account3.MaxAccount;
					}
					if (num6 == num4)
					{
						break;
					}
				}
			}
			num4 -= num6;
			num2 = 0f;
			for (int m = 0; m < accountIndexForResource.Length; m++)
			{
				Account account4 = this.accounts[accountIndexForResource[m]];
				if (account4.MaxAccount < 0f || account4.MaxAccount > account4.EstimatedBalance)
				{
					num2 += this.accounts[accountIndexForResource[m]].CurrentProfitPercent;
				}
			}
		}
		if (num5 >= 100)
		{
			num2 = 0f;
			for (int n = 0; n < accountIndexForResource.Length; n++)
			{
				num2 += this.accounts[accountIndexForResource[n]].CurrentProfitPercent;
			}
			for (int num8 = 0; num8 < accountIndexForResource.Length; num8++)
			{
				Account account5 = this.accounts[accountIndexForResource[num8]];
				account5.EstimatedBalance = num3 * (this.accounts[accountIndexForResource[num8]].CurrentProfitPercent / num2);
			}
			Diagnostics.LogWarning("Account manager took too much time. Empire = {0}, resource = {1}", new object[]
			{
				base.AIEntity.Empire.Index,
				resourceName
			});
		}
	}

	private void ValidateAccountMessages(Account account)
	{
		account.WantedAmount = 0f;
		this.workingMessages.Clear();
		float num = account.GetAvailableAmount();
		if (num <= 0f)
		{
			return;
		}
		Func<EvaluableMessage, bool> filter = (EvaluableMessage match) => match.AccountTag == account.AccountTag && (match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining);
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.City, filter));
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.Empire, filter));
		if (this.workingMessages.Count == 0)
		{
			return;
		}
		int[] bestEvaluationIndex = new int[this.workingMessages.Count];
		int[] array = new int[this.workingMessages.Count];
		for (int i = 0; i < this.workingMessages.Count; i++)
		{
			EvaluableMessage evaluableMessage = this.workingMessages[i];
			array[i] = i;
			bestEvaluationIndex[i] = -1;
			float num2 = 0f;
			for (int j = 0; j < evaluableMessage.BuyEvaluations.Count; j++)
			{
				BuyEvaluation buyEvaluation = evaluableMessage.BuyEvaluations[j];
				if (evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining || buyEvaluation == null || evaluableMessage.ChosenProductionEvaluation == null || !(buyEvaluation.CityGuid != evaluableMessage.ChosenProductionEvaluation.CityGuid))
				{
					buyEvaluation.ComputeFinalBuyoutScore(evaluableMessage.Interest, account.GetAvailableAmount());
					if (num2 < buyEvaluation.BuyoutFinalScore)
					{
						bestEvaluationIndex[i] = j;
						num2 = buyEvaluation.BuyoutFinalScore;
					}
				}
			}
		}
		Array.Sort<int>(array, delegate(int left, int right)
		{
			if (bestEvaluationIndex[left] == -1)
			{
				return 1;
			}
			if (bestEvaluationIndex[right] == -1)
			{
				return -1;
			}
			return -1 * this.workingMessages[left].BuyEvaluations[bestEvaluationIndex[left]].BuyoutFinalScore.CompareTo(this.workingMessages[right].BuyEvaluations[bestEvaluationIndex[right]].BuyoutFinalScore);
		});
		int num3 = 3;
		for (int k = 0; k < array.Length; k++)
		{
			int num4 = array[k];
			EvaluableMessage evaluableMessage2 = this.workingMessages[num4];
			int num5 = bestEvaluationIndex[num4];
			if (num5 >= 0 && num5 < evaluableMessage2.BuyEvaluations.Count)
			{
				BuyEvaluation buyEvaluation2 = evaluableMessage2.BuyEvaluations[num5];
				if (num < buyEvaluation2.DustCost)
				{
					if (k == 0)
					{
						account.WantedAmount = buyEvaluation2.DustCost;
					}
					if (--num3 < 0)
					{
						break;
					}
					num -= account.GetAvailableAmount() * this.percentRetainWhenNotEnough;
					if (num <= 0f)
					{
						break;
					}
				}
				else if (buyEvaluation2.BuyoutFinalScore > 0f)
				{
					if (evaluableMessage2.ChosenBuyEvaluation != null)
					{
						account.PromisedAmount -= evaluableMessage2.ChosenBuyEvaluation.DustCost;
					}
					evaluableMessage2.ValidateBuyEvaluation(buyEvaluation2);
					account.PromisedAmount += buyEvaluation2.DustCost;
					num -= buyEvaluation2.DustCost;
				}
			}
		}
	}

	private void ValidateAccountMessagesByResource(StaticString resourceName)
	{
		int[] array = this.accountIndexByResource[resourceName];
		for (int i = 0; i < array.Length; i++)
		{
			this.accounts[array[i]].WantedAmount = 0f;
			this.accounts[array[i]].WantedAmountPriority = 0f;
			this.accounts[array[i]].PromisedAmount = 0f;
		}
		this.GatherMessages(array);
		if (this.workingMessages.Count == 0)
		{
			return;
		}
		int[] bestEvaluationIndex = new int[this.workingMessages.Count];
		int[] array2 = new int[this.workingMessages.Count];
		float availableResourceStock = this.aiLayerEconomy.GetAvailableResourceStock(resourceName);
		float num = availableResourceStock;
		for (int j = 0; j < array.Length; j++)
		{
			this.accounts[array[j]].EstimatedBalance = availableResourceStock;
		}
		for (int k = 0; k < this.workingMessages.Count; k++)
		{
			EvaluableMessage evaluableMessage = this.workingMessages[k];
			array2[k] = k;
			bestEvaluationIndex[k] = -1;
			float num2 = 0f;
			for (int l = 0; l < evaluableMessage.BuyEvaluations.Count; l++)
			{
				BuyEvaluation buyEvaluation = evaluableMessage.BuyEvaluations[l];
				if (evaluableMessage.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining || buyEvaluation == null || evaluableMessage.ChosenProductionEvaluation == null || !(buyEvaluation.CityGuid != evaluableMessage.ChosenProductionEvaluation.CityGuid))
				{
					buyEvaluation.ComputeFinalBuyoutScore(evaluableMessage.Interest, num);
					if (num2 < buyEvaluation.BuyoutFinalScore)
					{
						bestEvaluationIndex[k] = l;
						num2 = buyEvaluation.BuyoutFinalScore;
					}
				}
			}
		}
		Array.Sort<int>(array2, delegate(int left, int right)
		{
			if (bestEvaluationIndex[left] == -1)
			{
				return 1;
			}
			if (bestEvaluationIndex[right] == -1)
			{
				return -1;
			}
			return -1 * this.workingMessages[left].BuyEvaluations[bestEvaluationIndex[left]].BuyoutFinalScore.CompareTo(this.workingMessages[right].BuyEvaluations[bestEvaluationIndex[right]].BuyoutFinalScore);
		});
		for (int m = 0; m < this.workingMessages.Count; m++)
		{
			EvaluableMessage evaluableMessage2 = this.workingMessages[m];
			if (evaluableMessage2.ChosenBuyEvaluation != null)
			{
				if (num < evaluableMessage2.ChosenBuyEvaluation.DustCost)
				{
					evaluableMessage2.RevokeChosenBuyEvaluation();
				}
				else
				{
					num -= evaluableMessage2.ChosenBuyEvaluation.DustCost;
					Account account = this.accounts[this.EnsureAccount(evaluableMessage2.AccountTag)];
					account.PromisedAmount += evaluableMessage2.ChosenBuyEvaluation.DustCost;
				}
			}
		}
		for (int n = 0; n < array2.Length; n++)
		{
			if (num <= 0f)
			{
				break;
			}
			int num3 = array2[n];
			EvaluableMessage evaluableMessage3 = this.workingMessages[num3];
			int num4 = bestEvaluationIndex[num3];
			if (num4 >= 0 && num4 < evaluableMessage3.BuyEvaluations.Count)
			{
				BuyEvaluation buyEvaluation2 = evaluableMessage3.BuyEvaluations[num4];
				Account account2 = this.accounts[this.EnsureAccount(evaluableMessage3.AccountTag)];
				if (num < buyEvaluation2.DustCost)
				{
					if (n == 0)
					{
						account2.WantedAmount = buyEvaluation2.DustCost;
						account2.WantedAmountPriority = buyEvaluation2.BuyoutFinalScore;
					}
					num -= account2.GetAvailableAmount() * this.percentRetainWhenNotEnough;
				}
				else if (buyEvaluation2.BuyoutFinalScore > 0f)
				{
					evaluableMessage3.ValidateBuyEvaluation(buyEvaluation2);
					account2.PromisedAmount += buyEvaluation2.DustCost;
					num -= buyEvaluation2.DustCost;
				}
			}
		}
	}

	private void GatherMessages(int[] accountIndexForResource)
	{
		this.workingMessages.Clear();
		Func<EvaluableMessage, bool> filter = (EvaluableMessage match) => Array.Exists<int>(accountIndexForResource, (int accountIndex) => accountIndex == this.EnsureAccount(match.AccountTag)) && (match.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || match.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate);
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.City, filter));
		this.workingMessages.AddRange(base.AIEntity.AIPlayer.Blackboard.GetMessages<EvaluableMessage>(BlackboardLayerID.Empire, filter));
	}

	private StaticString ResourceFromAccountTag(StaticString accountTag)
	{
		foreach (KeyValuePair<StaticString, int[]> keyValuePair in this.accountIndexByResource)
		{
			for (int i = 0; i < keyValuePair.Value.Length; i++)
			{
				if (this.accounts[keyValuePair.Value[i]].AccountTag == accountTag)
				{
					return keyValuePair.Key;
				}
			}
		}
		return StaticString.Empty;
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_AccountManager";

	public static StaticString EmpirePlanAccountName = "EmpirePlanAccount";

	public static StaticString AssimilationAccountName = AILayer_AccountManager.EmpirePlanAccountName;

	public static StaticString ConversionAccountName = AILayer_AccountManager.EmpirePlanAccountName;

	public static StaticString DiplomacyAccountName = "DiplomacyAccount";

	public static StaticString EconomyAccountName = "EconomyAccount";

	public static StaticString HeroAccountName = "HeroAccount";

	public static StaticString MilitaryAccountName = "MilitaryAccount";

	public static StaticString NoAccountName = "NoAccount";

	public static StaticString OrbAccountName = "OrbAccount";

	private Dictionary<StaticString, int[]> accountIndexByResource = new Dictionary<StaticString, int[]>();

	private List<Account> accounts = new List<Account>();

	private List<EvaluableMessage> workingMessages = new List<EvaluableMessage>();

	[InfluencedByPersonality]
	private float percentRetainWhenNotEnough = 0.8f;

	private AILayer_Economy aiLayerEconomy;
}
