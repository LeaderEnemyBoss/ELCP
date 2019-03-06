using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;

[PersonalityRegistryPath("AI/MajorEmpire/AIEntity_Empire/AILayer_UnitRecruitment", new object[]
{

})]
public class AILayer_UnitRecruitment : AILayer
{
	public Recruiter NavalRecruiter { get; set; }

	public Recruiter LandRecruiter { get; set; }

	private void MilitaryProduction()
	{
		float recruiterProductionPercent = 1f - this.navyLayer.NavyImportance;
		this.LandRecruiter.Produce((float)this.empireData.LandMilitaryStandardUnitCount, recruiterProductionPercent);
	}

	private void NavalProduction()
	{
		float recruiterProductionPercent = this.navyLayer.NavyImportance;
		this.NavalRecruiter.Produce((float)this.empireData.NavalMilitaryStandardUnitCount, recruiterProductionPercent);
	}

	private void ComputeWantedMilitaryUnitCount()
	{
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		this.LandRecruiter.WantedUnitCount.Reset();
		this.NavalRecruiter.WantedUnitCount.Reset();
		DepartmentOfTheInterior agency = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		if (agency != null)
		{
			HeuristicValue heuristicValue = new HeuristicValue(0f);
			heuristicValue.Add((float)agency.Cities.Count, "Number of city", new object[0]);
			heuristicValue.Multiply(1.2f, "constant", new object[0]);
			this.LandRecruiter.WantedUnitCount.Add(heuristicValue, "1.2f per owned land region", new object[0]);
			HeuristicValue heuristicValue2 = new HeuristicValue(0f);
			heuristicValue2.Add((float)agency.OccupiedFortresses.Count, "Number of fortress", new object[0]);
			heuristicValue2.Multiply(0.5f, "constant", new object[0]);
			this.NavalRecruiter.WantedUnitCount.Add(heuristicValue2, "Half per owned fortresses", new object[0]);
		}
		MajorEmpire majorEmpire = base.AIEntity.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			HeuristicValue heuristicValue3 = new HeuristicValue(0f);
			heuristicValue3.Add((float)majorEmpire.ConvertedVillages.Count, "Number of village", new object[0]);
			heuristicValue3.Multiply(0.5f, "constant", new object[0]);
			this.LandRecruiter.WantedUnitCount.Add(heuristicValue3, "Half per converted village", new object[0]);
		}
		DepartmentOfForeignAffairs agency2 = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		if (agency2 != null)
		{
			HeuristicValue heuristicValue4 = new HeuristicValue(0f);
			heuristicValue4.Add((float)agency.Cities.Count, "Number of city", new object[0]);
			heuristicValue4.Multiply((float)agency2.CountNumberOfWar(), "Number of war", new object[0]);
			heuristicValue4.Multiply(0.5f, "constant", new object[0]);
			this.LandRecruiter.WantedUnitCount.Add(heuristicValue4, "Half per city per war.", new object[0]);
		}
		AILayer_Navy layer = base.AIEntity.GetLayer<AILayer_Navy>();
		if (layer != null)
		{
			this.NavalRecruiter.WantedUnitCount.Add(layer.WantedArmies(), "Navy wanted army count", new object[0]);
		}
		AILayer_Colonization layer2 = base.AIEntity.GetLayer<AILayer_Colonization>();
		if (layer2 != null)
		{
			this.LandRecruiter.WantedUnitCount.Add((float)(layer2.WantedNewCity / 2), "Half per wanted region", new object[0]);
		}
		if (this.LandRecruiter.WantedUnitCount == 0f)
		{
			this.LandRecruiter.WantedUnitCount.Add(1f, "avoid 0", new object[0]);
		}
		if (this.NavalRecruiter.WantedUnitCount == 0f)
		{
			this.NavalRecruiter.WantedUnitCount.Add(1f, "avoid 0", new object[0]);
		}
		this.LandRecruiter.WantedUnitCount.Multiply(propertyValue, "Army size", new object[0]);
		this.NavalRecruiter.WantedUnitCount.Multiply(propertyValue, "Army size", new object[0]);
		float propertyValue2 = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.GameSpeedMultiplier);
		if ((float)this.endTurnService.Turn < 30f * propertyValue2)
		{
			this.LandRecruiter.WantedUnitCount.Multiply(0.3f, "Early game factor", new object[0]);
			this.NavalRecruiter.WantedUnitCount.Multiply(0.3f, "Early game factor", new object[0]);
		}
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.navyLayer = base.AIEntity.GetLayer<AILayer_Navy>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		base.AIEntity.RegisterPass(AIEntity.Passes.CreateLocalNeeds.ToString(), "AILayer_UnitRecruitment_CreateLocalNeedsPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[]
		{
			"AILayerArmyRecruitment_CreateLocalNeedsPass"
		});
		base.AIEntity.RegisterPass(AIEntity.Passes.EvaluateNeeds.ToString(), "AILayer_UnitRecruitment_EvaluateNeedPass", new AIEntity.AIAction(this.CreateLocalNeeds), this, new StaticString[0]);
		InfluencedByPersonalityAttribute.LoadFieldAndPropertyValues(base.AIEntity.Empire, this);
		this.LandRecruiter = new Recruiter();
		this.LandRecruiter.AIEntity = base.AIEntity;
		this.LandRecruiter.UnitDesignFilter = new Func<UnitDesign, bool>(this.UnitDesignFilter_LandMilitaryUnit);
		this.LandRecruiter.Initialize();
		this.NavalRecruiter = new Recruiter();
		this.NavalRecruiter.AIEntity = base.AIEntity;
		this.NavalRecruiter.UnitDesignFilter = new Func<UnitDesign, bool>(this.UnitDesignFilter_NavyMilitaryUnit);
		this.NavalRecruiter.Initialize();
		this.VictoryLayer = base.AIEntity.GetLayer<AILayer_Victory>();
		this.ColonizationLayer = base.AIEntity.GetLayer<AILayer_Colonization>();
		yield break;
	}

	public override void Release()
	{
		base.Release();
		this.navyLayer = null;
		this.endTurnService = null;
		if (this.LandRecruiter != null)
		{
			this.LandRecruiter.UnitDesignFilter = null;
			this.LandRecruiter = null;
		}
		if (this.NavalRecruiter != null)
		{
			this.NavalRecruiter.UnitDesignFilter = null;
			this.NavalRecruiter = null;
		}
		this.requestArmyMessages.Clear();
		this.VictoryLayer = null;
		this.ColonizationLayer = null;
	}

	protected override void CreateLocalNeeds(StaticString context, StaticString pass)
	{
		base.CreateLocalNeeds(context, pass);
		if (!AIScheduler.Services.GetService<IAIEmpireDataAIHelper>().TryGet(base.AIEntity.Empire.Index, out this.empireData))
		{
			return;
		}
		base.AIEntity.AIPlayer.Blackboard.FillMessages<RequestUnitListMessage>(BlackboardLayerID.Empire, (RequestUnitListMessage match) => match.EmpireTarget == base.AIEntity.Empire.Index, ref this.requestArmyMessages);
		this.ComputeWantedMilitaryUnitCount();
		this.MilitaryProduction();
		this.NavalProduction();
		this.SettlerProduction();
	}

	protected override void EvaluateNeeds(StaticString context, StaticString pass)
	{
		base.EvaluateNeeds(context, pass);
	}

	private void SettlerProduction()
	{
		UnitDesign unitDesign = this.FindSettlerDesign();
		if (unitDesign == null)
		{
			return;
		}
		List<EvaluableMessage_SettlerProduction> list = new List<EvaluableMessage_SettlerProduction>();
		base.AIEntity.AIPlayer.Blackboard.FillMessages<EvaluableMessage_SettlerProduction>(BlackboardLayerID.Empire, (EvaluableMessage_SettlerProduction match) => match.EvaluationState != EvaluableMessage.EvaluableMessageState.Cancel && match.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtained, ref list);
		HeuristicValue heuristicValue = new HeuristicValue(0f);
		heuristicValue.Add(1f, "(constant)", new object[0]);
		if (this.ColonizationLayer == null || this.ColonizationLayer.CurrentSettlerCount < 2)
		{
			for (int i = 0; i < this.requestArmyMessages.Count; i++)
			{
				if (this.requestArmyMessages[i].CommanderCategory == AICommanderMissionDefinition.AICommanderCategory.Colonization && this.requestArmyMessages[i].ExecutionState == RequestUnitListMessage.RequestUnitListState.Pending)
				{
					if (list.Count == 0)
					{
						HeuristicValue heuristicValue2 = new HeuristicValue(0f);
						heuristicValue2.Add(this.requestArmyMessages[i].Priority, "Army request priority", new object[0]);
						EvaluableMessage_SettlerProduction message = new EvaluableMessage_SettlerProduction(heuristicValue, heuristicValue2, unitDesign, -1, 1, AILayer_AccountManager.MilitaryAccountName);
						base.AIEntity.AIPlayer.Blackboard.AddMessage(message);
					}
					else
					{
						list[0].Refresh(1f, this.requestArmyMessages[i].Priority);
					}
				}
			}
		}
		if (this.VictoryLayer != null && this.VictoryLayer.NeedSettlers && (this.ColonizationLayer == null || this.ColonizationLayer.CurrentSettlerCount < 10))
		{
			if (list.Count < 1)
			{
				HeuristicValue localOpportunity = new HeuristicValue(1f);
				EvaluableMessage_SettlerProduction message2 = new EvaluableMessage_SettlerProduction(heuristicValue, localOpportunity, unitDesign, -1, 1, AILayer_AccountManager.MilitaryAccountName);
				base.AIEntity.AIPlayer.Blackboard.AddMessage(message2);
				return;
			}
			list[0].Refresh(1f, 1f);
		}
	}

	private UnitDesign FindSettlerDesign()
	{
		DepartmentOfDefense agency = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		for (int i = 0; i < agency.UnitDesignDatabase.UserDefinedUnitDesigns.Count; i++)
		{
			if (agency.UnitDesignDatabase.UserDefinedUnitDesigns[i].CheckUnitAbility(UnitAbility.ReadonlyColonize, -1))
			{
				return agency.UnitDesignDatabase.UserDefinedUnitDesigns[i];
			}
		}
		return null;
	}

	private bool UnitDesignFilter_LandMilitaryUnit(UnitDesign unitDesign)
	{
		return !unitDesign.CheckAgainstTag(DownloadableContent13.UnitTypeManta) && !unitDesign.CheckAgainstTag(DownloadableContent9.TagColossus) && !unitDesign.CheckAgainstTag(TradableUnit.ReadOnlyMercenary) && !unitDesign.CheckUnitAbility(UnitAbility.ReadonlyColonize, -1) && !unitDesign.CheckAgainstTag(DownloadableContent16.SeafaringUnit);
	}

	private bool UnitDesignFilter_NavyMilitaryUnit(UnitDesign unitDesign)
	{
		return unitDesign.CheckAgainstTag(DownloadableContent16.SeafaringUnit);
	}

	public const string RegistryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_UnitRecruitment";

	private AILayer_Navy navyLayer;

	private AIEmpireData empireData;

	private List<RequestUnitListMessage> requestArmyMessages = new List<RequestUnitListMessage>();

	private IEndTurnService endTurnService;

	private AILayer_Victory VictoryLayer;

	private AILayer_Colonization ColonizationLayer;
}
