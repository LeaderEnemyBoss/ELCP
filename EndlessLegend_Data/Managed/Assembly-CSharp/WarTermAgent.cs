using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;
using UnityEngine;

[PersonalityRegistryPath("AI/AgentDefinition/WarTermAgent/", new object[]
{

})]
public class WarTermAgent : DiplomaticTermAgent
{
	public override void Reset()
	{
		this.departmentOfForeignAffairs = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
		ISessionService service = Services.GetService<ISessionService>();
		this.SharedVictory = service.Session.GetLobbyData<bool>("Shared", true);
		GameServer gameServer = (service.Session as global::Session).GameServer as GameServer;
		AIPlayer_MajorEmpire aiplayer_MajorEmpire;
		if (!(base.EmpireWhichReceives as MajorEmpire).ELCPIsEliminated && gameServer != null && gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.EmpireWhichReceives as MajorEmpire, out aiplayer_MajorEmpire))
		{
			AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
			if (entity == null)
			{
				Diagnostics.LogError("ELCP {0} has no entity for agent of {1}", new object[]
				{
					base.EmpireWhichReceives,
					base.Empire
				});
			}
			this.otherDiplomacyLayer = entity.GetLayer<AILayer_Diplomacy>();
		}
		base.Reset();
		bool enable = base.Enable;
	}

	protected override void ComputeInitValue()
	{
		base.ComputeInitValue();
		base.ValueInit = Mathf.Clamp01(this.ComputeHeuristicBaseValue());
	}

	protected override void ComputeValue()
	{
		base.ComputeValue();
		float num = this.ComputeHeuristicBaseValue();
		if (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War)
		{
			num -= 0.2f * base.DiplomaticRelation.RelationStateChaosScore;
		}
		base.Value = Mathf.Clamp01(num);
	}

	private float ComputeHeuristicBaseValue()
	{
		Diagnostics.Assert(base.AttitudeScore != null);
		Diagnostics.Assert(base.DiplomaticRelation != null && base.DiplomaticRelation.State != null);
		if (base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.Unknown)
		{
			return 0f;
		}
		float num = base.GetValueFromAttitude();
		if (base.HasOtherEmpireClosedTheirBorders())
		{
			num += this.otherEmpireClosedBordersBonus;
		}
		else if (base.AreTradeRoutesPossibleWithEmpire())
		{
			num -= this.blockedTradePenalty;
		}
		if (!this.departmentOfForeignAffairs.IsInWarWithSomeone())
		{
			float propertyValue = base.Empire.GetPropertyValue(SimulationProperties.LandMilitaryPower);
			if (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Military && propertyValue > base.EmpireWhichReceives.GetPropertyValue(SimulationProperties.LandMilitaryPower) && (!this.SharedVictory || base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.Alliance))
			{
				num = Mathf.Max(70f, num + 70f);
			}
			if (!(base.EmpireWhichReceives as MajorEmpire).ELCPIsEliminated && this.opportunistMultiplier != 1f && base.EmpireWhichReceives.GetPropertyValue(SimulationProperties.WarCount) > 0f && !this.DiplomacyLayer.GetPeaceWish(base.EmpireWhichReceives.Index) && propertyValue > this.otherDiplomacyLayer.GetMilitaryPowerDif(false) * 1.5f)
			{
				num = Mathf.Max(100f * (this.opportunistMultiplier - 1f), num * this.opportunistMultiplier);
			}
		}
		num *= this.multiplier;
		if (!this.DiplomacyLayer.NeedsVictoryReaction[base.EmpireWhichReceives.Index])
		{
			if (this.DiplomacyLayer.AnyVictoryreactionNeeded && this.VictoryLayer.CurrentFocusEnum != AILayer_Victory.VictoryFocus.Military)
			{
				num /= 1.5f;
				num = Math.Min(num, 70f);
			}
			if (this.VictoryLayer.CurrentFocusEnum == AILayer_Victory.VictoryFocus.Diplomacy)
			{
				num = Mathf.Min(60f, num);
			}
			float propertyValue2 = base.Empire.GetPropertyValue(SimulationProperties.WarCount);
			if (this.DiplomacyLayer.GetMilitaryPowerDif(false) <= 0f && ((base.DiplomaticRelation.State.Name == DiplomaticRelationState.Names.War && propertyValue2 > 1f) || (base.DiplomaticRelation.State.Name != DiplomaticRelationState.Names.War && propertyValue2 > 0f)))
			{
				num = Mathf.Min(40f, num / 2f);
			}
		}
		return num / 100f;
	}

	public override void Release()
	{
		this.departmentOfForeignAffairs = null;
		this.otherDiplomacyLayer = null;
		base.Release();
	}

	public bool IsOpportunist
	{
		get
		{
			return this.opportunistMultiplier > 1f;
		}
	}

	[InfluencedByPersonality]
	private float blockedTradePenalty = 40f;

	[InfluencedByPersonality]
	private float otherEmpireClosedBordersBonus = 40f;

	[InfluencedByPersonality]
	private float multiplier = 1f;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private bool SharedVictory;

	[InfluencedByPersonality]
	private float opportunistMultiplier = 1f;

	private AILayer_Diplomacy otherDiplomacyLayer;
}
