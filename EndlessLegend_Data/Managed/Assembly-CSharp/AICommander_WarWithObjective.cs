using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;
using UnityEngine;

public class AICommander_WarWithObjective : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_WarWithObjective(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.War, globalObjectiveID, regionIndex)
	{
		this.CurrentWarStep = AICommander_WarWithObjective.WarSteps.GoingToFrontier;
	}

	public AICommander_WarWithObjective() : base(AICommanderMissionDefinition.AICommanderCategory.War, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		this.CurrentWarStep = (AICommander_WarWithObjective.WarSteps)reader.GetAttribute<int>("CurrentWarStep");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("CurrentWarStep", (int)this.CurrentWarStep);
		base.WriteXml(writer);
	}

	public AICommander_WarWithObjective.WarSteps CurrentWarStep { get; set; }

	public bool NeedPrivateersHarrasementMission { get; set; }

	public override float GetPriority(AICommanderMission mission)
	{
		if (base.Missions.Count > 0)
		{
			int num = base.Missions.FindIndex((AICommanderMission match) => match == mission);
			if (num >= 0 && base.Missions[num].AIDataArmyGUID.IsValid)
			{
				return 0.8f;
			}
		}
		return base.GlobalPriority * base.LocalPriority;
	}

	public override void Initialize()
	{
		base.Initialize();
		Diagnostics.Assert(AIScheduler.Services != null);
		IEntityInfoAIHelper service = AIScheduler.Services.GetService<IEntityInfoAIHelper>();
		Diagnostics.Assert(service != null);
		this.CurrentWarStep = AICommander_WarWithObjective.WarSteps.GoingToFrontier;
		IGameService service2 = Services.GetService<IGameService>();
		this.worldPositionningService = service2.Game.Services.GetService<IWorldPositionningService>();
		this.aiDataRepository = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.empireDataRepository = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.intelligenceAIHelper = AIScheduler.Services.GetService<IIntelligenceAIHelper>();
		this.personalityAIHelper = AIScheduler.Services.GetService<IPersonalityAIHelper>();
		this.cityMilitaryPowerFactor = this.personalityAIHelper.GetRegistryValue<float>(base.Empire, string.Format("{0}/{1}", AICommander_WarWithObjective.registryPath, "CityMilitaryPowerFactor"), this.cityMilitaryPowerFactor);
		this.departmentOfScience = base.Empire.GetAgency<DepartmentOfScience>();
	}

	public override bool IsMissionFinished(bool forceStep)
	{
		if (this.IsMissionFinished() || base.IsMissionFinished(forceStep))
		{
			return true;
		}
		for (int i = 0; i < base.Missions.Count; i++)
		{
			if (!forceStep || base.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Initializing)
			{
				if (base.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Fail && base.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Interrupted && base.Missions[i].Completion != AICommanderMission.AICommanderMissionCompletion.Cancelled)
				{
					return false;
				}
			}
		}
		return true;
	}

	public override void PopulateMission()
	{
		if (this.IsMissionFinished())
		{
			return;
		}
		Region region = this.worldPositionningService.GetRegion(base.RegionIndex);
		Tags tags = new Tags();
		tags.AddTag(base.Category.ToString());
		if (this.CurrentWarStep == AICommander_WarWithObjective.WarSteps.GoingToFrontier)
		{
			DepartmentOfForeignAffairs agency = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			DiplomaticRelation diplomaticRelation = agency.DiplomaticRelations[region.City.Empire.Index];
			if (diplomaticRelation != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
			{
				this.CurrentWarStep = AICommander_WarWithObjective.WarSteps.BeSiegingCity;
				AICommanderMission aicommanderMission = base.Missions.Find((AICommanderMission match) => match is AICommanderMission_FrontierHarass);
				if (aicommanderMission != null)
				{
					this.CancelMission(aicommanderMission);
				}
			}
		}
		if (this.NeedPrivateersHarrasementMission && this.CurrentWarStep == AICommander_WarWithObjective.WarSteps.GoingToFrontier)
		{
			if (!base.Missions.Exists((AICommanderMission match) => match is AICommanderMission_PrivateersHarass))
			{
				tags.AddTag("PrivateersHarass");
				base.PopulationFirstMissionFromCategory(tags, new object[]
				{
					region.City
				});
				tags.RemoveTag("PrivateersHarass");
			}
		}
		else
		{
			AICommanderMission aicommanderMission2 = base.Missions.Find((AICommanderMission match) => match is AICommanderMission_PrivateersHarass);
			if (aicommanderMission2 != null)
			{
				this.CancelMission(aicommanderMission2);
			}
		}
		AICommander_WarWithObjective.WarSteps currentWarStep = this.CurrentWarStep;
		if (currentWarStep != AICommander_WarWithObjective.WarSteps.GoingToFrontier)
		{
			if (currentWarStep != AICommander_WarWithObjective.WarSteps.BeSiegingCity)
			{
				Diagnostics.LogError(string.Format("[AICommander_WarWithObjective] Unknow war step", this.CurrentWarStep.ToString()));
			}
			else
			{
				this.PopulateBesiegingCityMission(tags, region);
				if (region.City.Camp != null)
				{
					this.PopulateAttackCampMission(tags, region);
				}
				else
				{
					List<AICommanderMission> list = base.Missions.FindAll((AICommanderMission match) => match is AICommanderMission_AttackCampDefault);
					if (list.Count > 0)
					{
						for (int i = 0; i < list.Count; i++)
						{
							this.CancelMission(list[i]);
						}
					}
				}
			}
		}
		else if (!base.Missions.Exists((AICommanderMission match) => match is AICommanderMission_FrontierHarass))
		{
			tags.AddTag("FrontierHarass");
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				region
			});
		}
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		if (this.IsMissionFinished())
		{
			return;
		}
		this.NeedPrivateersHarrasementMission = false;
		MajorEmpire majorEmpire = base.Empire as MajorEmpire;
		if (majorEmpire != null)
		{
			Region region2 = this.worldPositionningService.GetRegion(base.RegionIndex);
			DepartmentOfForeignAffairs agency = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
			Diagnostics.Assert(agency != null);
			DiplomaticRelation diplomaticRelation = agency.DiplomaticRelations[region2.City.Empire.Index];
			if (diplomaticRelation != null && diplomaticRelation.State.Name == DiplomaticRelationState.Names.War)
			{
				this.NeedPrivateersHarrasementMission = false;
			}
			else
			{
				this.NeedPrivateersHarrasementMission = (majorEmpire.SimulationObject.Tags.Contains(AILayer_War.TagNoWarTrait) && this.departmentOfScience.CanCreatePrivateers());
			}
		}
		if (base.Missions.Count != 0)
		{
			Region region = this.worldPositionningService.GetRegion(base.RegionIndex);
			switch (this.CurrentWarStep)
			{
			case AICommander_WarWithObjective.WarSteps.GoingToFrontier:
			{
				DepartmentOfForeignAffairs agency2 = base.Empire.GetAgency<DepartmentOfForeignAffairs>();
				Diagnostics.Assert(agency2 != null);
				DiplomaticRelation diplomaticRelation2 = agency2.DiplomaticRelations[region.City.Empire.Index];
				if (diplomaticRelation2.State == null)
				{
					Diagnostics.LogError("[AICommander_WarWithObjective] DiplomaticRelation is null");
					return;
				}
				AICommanderMission aicommanderMission = base.Missions.Find((AICommanderMission match) => match is AICommanderMission_FrontierHarass);
				AICommanderMission_FrontierHarass aicommanderMission_FrontierHarass = aicommanderMission as AICommanderMission_FrontierHarass;
				if (aicommanderMission_FrontierHarass != null && aicommanderMission_FrontierHarass.Completion != AICommanderMission.AICommanderMissionCompletion.Fail && aicommanderMission_FrontierHarass.Completion != AICommanderMission.AICommanderMissionCompletion.Interrupted && aicommanderMission_FrontierHarass.Completion != AICommanderMission.AICommanderMissionCompletion.Cancelled)
				{
					if (aicommanderMission_FrontierHarass.ArrivedToDestination())
					{
						if (diplomaticRelation2.State.Name == DiplomaticRelationState.Names.War)
						{
							if (this.aiDataRepository.GetAIData<AIData_Army>(aicommanderMission_FrontierHarass.AIDataArmyGUID) == null)
							{
								aicommanderMission_FrontierHarass.Interrupt();
							}
							else
							{
								base.ForceArmyGUID = aicommanderMission_FrontierHarass.AIDataArmyGUID;
								this.CurrentWarStep = AICommander_WarWithObjective.WarSteps.BeSiegingCity;
								this.CancelMission(aicommanderMission_FrontierHarass);
							}
						}
						else
						{
							WantedDiplomaticRelationStateMessage wantedDiplomaticRelationStateMessage = base.AIPlayer.Blackboard.FindFirst<WantedDiplomaticRelationStateMessage>(BlackboardLayerID.Empire, (WantedDiplomaticRelationStateMessage message) => message.OpponentEmpireIndex == region.City.Empire.Index);
							if (wantedDiplomaticRelationStateMessage == null)
							{
								Diagnostics.LogError("[AICommander_WarWithObjective] No WantedDiplomaticRelationStateMessage found");
							}
							else if (wantedDiplomaticRelationStateMessage.WantedDiplomaticRelationStateName == DiplomaticRelationState.Names.War)
							{
								wantedDiplomaticRelationStateMessage.CurrentWarStatusType = AILayer_War.WarStatusType.Ready;
							}
						}
					}
				}
				break;
			}
			case AICommander_WarWithObjective.WarSteps.BeSiegingCity:
				this.BesiegeCityProcessor(region);
				break;
			case AICommander_WarWithObjective.WarSteps.AttackingCity:
			{
				AICommanderMission aicommanderMission2 = base.Missions.Find((AICommanderMission match) => match is AICommanderMission_AttackCityDefault);
				if (aicommanderMission2 != null)
				{
					base.ForceArmyGUID = aicommanderMission2.AIDataArmyGUID;
					this.CancelMission(aicommanderMission2);
				}
				this.CurrentWarStep = AICommander_WarWithObjective.WarSteps.BeSiegingCity;
				break;
			}
			default:
				Diagnostics.LogError(string.Format("[AICommander_WarWithObjective] Unknow war step", this.CurrentWarStep.ToString()));
				break;
			}
		}
		this.PopulateMission();
		this.EvaluateMission();
		base.PromoteMission();
	}

	private void BesiegeCityProcessor(Region region)
	{
	}

	private bool IsMissionFinished()
	{
		Region region = this.worldPositionningService.GetRegion(base.RegionIndex);
		return region == null || region.City == null || region.City.Empire == base.Empire || (region.City.BesiegingEmpire != null && region.City.BesiegingEmpire != base.Empire);
	}

	private void PopulateBesiegingCityMission(Tags tags, Region region)
	{
		float armyMaxPower = this.GetArmyMaxPower();
		int num = Mathf.CeilToInt(this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Empire, region.City, 0) * this.cityMilitaryPowerFactor / armyMaxPower);
		if (num == 0)
		{
			num = 1;
		}
		else if (num > 5)
		{
			num = 5;
		}
		int num2 = 0;
		int num3 = 0;
		for (int i = 0; i < base.Missions.Count; i++)
		{
			AICommanderMission_BesiegeCityDefault aicommanderMission_BesiegeCityDefault = base.Missions[i] as AICommanderMission_BesiegeCityDefault;
			if (aicommanderMission_BesiegeCityDefault != null)
			{
				if (num2 < num)
				{
					aicommanderMission_BesiegeCityDefault.IsReinforcement = false;
					if (!aicommanderMission_BesiegeCityDefault.AIDataArmyGUID.IsValid)
					{
						num3++;
					}
				}
				else
				{
					if (num2 >= num + 2)
					{
						this.CancelMission(aicommanderMission_BesiegeCityDefault);
						goto IL_9C;
					}
					aicommanderMission_BesiegeCityDefault.IsReinforcement = true;
				}
				num2++;
			}
			IL_9C:;
		}
		GlobalObjectiveMessage globalObjectiveMessage2;
		if (num2 - num3 >= num)
		{
			GlobalObjectiveMessage globalObjectiveMessage;
			if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage))
			{
				globalObjectiveMessage.ObjectiveState = "Attacking";
			}
		}
		else if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage2))
		{
			globalObjectiveMessage2.ObjectiveState = "Preparing";
		}
		for (int j = num2; j < num; j++)
		{
			tags.AddTag("BesiegeCity");
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				region.City
			});
		}
	}

	private void PopulateAttackCampMission(Tags tags, Region region)
	{
		float armyMaxPower = this.GetArmyMaxPower();
		float num = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Empire, region.City.Camp, 0);
		num *= this.cityMilitaryPowerFactor;
		int num2 = Mathf.CeilToInt(num / armyMaxPower);
		if (num2 == 0)
		{
			num2 = 1;
		}
		else if (num2 > 2)
		{
			num2 = 2;
		}
		int num3 = 0;
		int num4 = 0;
		for (int i = 0; i < base.Missions.Count; i++)
		{
			AICommanderMission_AttackCampDefault aicommanderMission_AttackCampDefault = base.Missions[i] as AICommanderMission_AttackCampDefault;
			if (aicommanderMission_AttackCampDefault != null)
			{
				if (num3 < num2)
				{
					aicommanderMission_AttackCampDefault.IsReinforcement = false;
					if (!aicommanderMission_AttackCampDefault.AIDataArmyGUID.IsValid)
					{
						num4++;
					}
				}
				else
				{
					if (num3 >= num2 + 1)
					{
						this.CancelMission(aicommanderMission_AttackCampDefault);
						goto IL_C6;
					}
					aicommanderMission_AttackCampDefault.IsReinforcement = true;
				}
				num3++;
			}
			IL_C6:;
		}
		for (int j = num3; j < num2; j++)
		{
			tags.AddTag("AttackCamp");
			base.PopulationFirstMissionFromCategory(tags, new object[]
			{
				region.City.Camp
			});
		}
	}

	private float GetArmyMaxPower()
	{
		AIEmpireData aiempireData;
		if (this.empireDataRepository.TryGet(base.Empire.Index, out aiempireData))
		{
			return aiempireData.AverageUnitDesignMilitaryPower * base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot) * base.Empire.GetPropertyValue(SimulationProperties.ArmyUnitSlot);
		}
		return float.MaxValue;
	}

	private const int maxArmiesBesieging = 3;

	private const int maxArmiesAttackingCamp = 2;

	private static string registryPath = "AI/MajorEmpire/AIEntity_Empire/AILayer_War/AICommander";

	private float cityMilitaryPowerFactor = 1.2f;

	private DepartmentOfScience departmentOfScience;

	private IAIEmpireDataAIHelper empireDataRepository;

	private IIntelligenceAIHelper intelligenceAIHelper;

	private IPersonalityAIHelper personalityAIHelper;

	private IWorldPositionningService worldPositionningService;

	private IAIDataRepositoryAIHelper aiDataRepository;

	public enum WarSteps
	{
		GoingToFrontier,
		BeSiegingCity,
		AttackingCity
	}
}
