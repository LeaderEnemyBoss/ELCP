using System;
using Amplitude;
using Amplitude.Unity.AI.Amas;
using Amplitude.Unity.AI.Amas.Simulation;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/EconomyAccountNeedMoneyRatioAgent/", new object[]
{

})]
public class EconomyAccountNeedMoneyRatioAgent : SimulationAgent
{
	public override void Initialize(AgentDefinition agentDefinition, AgentGroup parentGroup)
	{
		base.Initialize(agentDefinition, parentGroup);
		AIPlayer_MajorEmpire aiplayer_MajorEmpire = base.ContextObject as AIPlayer_MajorEmpire;
		if (aiplayer_MajorEmpire == null)
		{
			Diagnostics.LogError("The agent context object is not an ai player.");
			return;
		}
		this.empire = aiplayer_MajorEmpire.MajorEmpire;
		if (this.empire == null)
		{
			Diagnostics.LogError("Can't retrieve the empire.");
			return;
		}
		this.aiEntityEmpire = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
		Diagnostics.Assert(this.aiEntityEmpire != null);
		Diagnostics.Assert(base.ParentGroup != null);
		this.netEmpireMoneyAgent = (base.ParentGroup.GetAgent("NetEmpireMoney") as SimulationNormalizedAgent);
		Diagnostics.Assert(this.netEmpireMoneyAgent != null);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(this.aiEntityEmpire.Empire, this);
	}

	public override void Release()
	{
		this.empire = null;
		this.aiEntityEmpire = null;
		this.netEmpireMoneyAgent = null;
		this.economyAccount = null;
		base.Release();
	}

	public override void Reset()
	{
		AILayer_AccountManager layer = this.aiEntityEmpire.GetLayer<AILayer_AccountManager>();
		if (layer == null)
		{
			base.Enable = false;
			return;
		}
		this.economyAccount = layer.TryGetAccount(AILayer_AccountManager.EconomyAccountName);
		if (this.economyAccount == null)
		{
			Diagnostics.LogError("Can't get resource account credit.");
			base.Enable = false;
			return;
		}
		this.moneyAccountCredit = this.economyAccount.GetAvailableAmount();
		this.moneyAccountRatio = this.economyAccount.CurrentProfitPercent;
		this.wantedMoney = this.economyAccount.WantedAmount;
		base.Reset();
		if (!base.Enable)
		{
			return;
		}
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		float num = this.wantedMoney - this.moneyAccountCredit;
		if (num <= 0f)
		{
			base.ValueInit = 0f;
			return;
		}
		float num2 = this.empire.GetPropertyValue(SimulationProperties.NetEmpireMoney) * this.moneyAccountRatio;
		float value = (num2 <= float.Epsilon) ? 1f : (num / (this.numberOfTurnToReachObjective * num2));
		base.ValueInit = Mathf.Clamp(value, base.ValueMin, base.ValueMax);
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		float num = this.wantedMoney - this.moneyAccountCredit;
		if (num <= 0f)
		{
			base.Value = 0f;
			return;
		}
		float num2 = this.netEmpireMoneyAgent.UnnormalizedValue * this.moneyAccountRatio;
		float value = (num2 <= float.Epsilon) ? 1f : (num / (this.numberOfTurnToReachObjective * num2));
		base.Value = Mathf.Clamp(value, base.ValueMin, base.ValueMax);
	}

	private AIEntity aiEntityEmpire;

	private Account economyAccount;

	private Empire empire;

	private float moneyAccountCredit;

	private float moneyAccountRatio;

	private SimulationNormalizedAgent netEmpireMoneyAgent;

	private float wantedMoney;

	[InfluencedByPersonality]
	private float numberOfTurnToReachObjective = 10f;
}
