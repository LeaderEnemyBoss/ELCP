using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class AILayer_Colossus : AILayerCommanderController
{
	public AILayer_Colossus() : base("Colossus")
	{
	}

	public ColossusMission UpdateArmySupportMission(ColossusMission currentColossusMission, Unit colossus)
	{
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.ArmySupport);
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			worldPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		Region region = this.worldPositioningService.GetRegion(worldPosition);
		float num = 0f;
		GameEntityGUID targetGuid = GameEntityGUID.Zero;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			if (currentColossusMission == null || !(this.departmentOfDefense.Armies[i].GUID == currentColossusMission.TargetGuid))
			{
				if (!this.hasShips)
				{
					Region region2 = this.worldPositioningService.GetRegion(this.departmentOfDefense.Armies[i].WorldPosition);
					if (region.ContinentID != region2.ContinentID)
					{
						goto IL_18C;
					}
				}
				float num2 = this.ComputeArmyFitness(this.departmentOfDefense.Armies[i].GUID, worldPosition);
				float num3 = num2 * fitnessForMission;
				ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(this.departmentOfDefense.Armies[i].GUID);
				if (colossusMissionPerTarget != null && currentColossusMission != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && num3 < colossusMissionPerTarget.Fitness)
				{
					num2 *= 0.5f;
					num3 *= 0.5f;
				}
				if (currentColossusMission == null || num3 >= currentColossusMission.Fitness)
				{
					if (num < num3)
					{
						num = num3;
						targetGuid = this.departmentOfDefense.Armies[i].GUID;
					}
				}
			}
			IL_18C:;
		}
		if (targetGuid.IsValid)
		{
			return new ColossusMission
			{
				EntityFitness = num,
				ColossusMissionFitness = fitnessForMission,
				TargetGuid = targetGuid,
				ColossusGuid = colossus.GUID,
				Objective = ColossusMission.MissionObjective.ArmySupport
			};
		}
		return null;
	}

	private float ComputeArmyFitness(GameEntityGUID armyGuid, WorldPosition colossusArmyPosition)
	{
		float num = 0f;
		AIData_Army aidata_Army;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(armyGuid, out aidata_Army))
		{
			num = aidata_Army.SupportScore;
			if (colossusArmyPosition.IsValid && num > 0f)
			{
				float num2 = (float)this.worldPositioningService.GetDistance(aidata_Army.Army.WorldPosition, colossusArmyPosition);
				float num3 = Mathf.Clamp01(num2 / this.averageMaximumMovementPoint / 5f);
				num = AILayer.Boost(num, num3 * -0.5f);
			}
		}
		return num;
	}

	private void RefreshArmySupportMission(ColossusMission currentColossusMission, Unit colossus)
	{
		if (currentColossusMission.Objective != ColossusMission.MissionObjective.ArmySupport)
		{
			return;
		}
		WorldPosition colossusArmyPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			colossusArmyPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.ArmySupport);
		float num = this.ComputeArmyFitness(currentColossusMission.TargetGuid, colossusArmyPosition);
		float num2 = num * fitnessForMission;
		ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(currentColossusMission.TargetGuid);
		if (colossusMissionPerTarget != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && colossusMissionPerTarget.ColossusGuid != colossus.GUID && num2 < colossusMissionPerTarget.Fitness)
		{
			num *= 0.5f;
		}
		num = AILayer.Boost(num, 0.1f);
		currentColossusMission.EntityFitness = num;
		currentColossusMission.ColossusMissionFitness = fitnessForMission;
	}

	public ColossusMission UpdateCityDefenseMission(ColossusMission currentColossusMission, Unit colossus)
	{
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.CityDefense);
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			worldPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		Region region = this.worldPositioningService.GetRegion(worldPosition);
		float num = 0f;
		float entityFitness = 0f;
		GameEntityGUID targetGuid = GameEntityGUID.Zero;
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			if (currentColossusMission == null || !(this.departmentOfTheInterior.Cities[i].GUID == currentColossusMission.TargetGuid))
			{
				if (!this.hasShips)
				{
					Region region2 = this.worldPositioningService.GetRegion(this.departmentOfTheInterior.Cities[i].WorldPosition);
					if (region.ContinentID != region2.ContinentID)
					{
						goto IL_193;
					}
				}
				float num2 = this.ComputeCityFitness(this.departmentOfTheInterior.Cities[i].GUID, worldPosition);
				float num3 = num2 * fitnessForMission;
				ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(this.departmentOfTheInterior.Cities[i].GUID);
				if (colossusMissionPerTarget != null && currentColossusMission != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && num3 < colossusMissionPerTarget.Fitness)
				{
					num2 *= 0.5f;
					num3 *= 0.5f;
				}
				if (currentColossusMission == null || num3 >= currentColossusMission.Fitness)
				{
					if (num < num3)
					{
						num = num3;
						targetGuid = this.departmentOfTheInterior.Cities[i].GUID;
					}
				}
			}
			IL_193:;
		}
		if (targetGuid.IsValid)
		{
			return new ColossusMission
			{
				EntityFitness = entityFitness,
				ColossusMissionFitness = fitnessForMission,
				TargetGuid = targetGuid,
				ColossusGuid = colossus.GUID,
				Objective = ColossusMission.MissionObjective.CityDefense
			};
		}
		return null;
	}

	private float ComputeCityFitness(GameEntityGUID cityGuid, WorldPosition colossusArmyPosition)
	{
		float num = 0f;
		AIData_City aidata_City;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(cityGuid, out aidata_City))
		{
			num = aidata_City.DefenseScore;
			if (colossusArmyPosition.IsValid)
			{
				float num2 = (float)this.worldPositioningService.GetDistance(aidata_City.City.WorldPosition, colossusArmyPosition);
				float num3 = Mathf.Clamp01(num2 / this.averageMaximumMovementPoint / 5f);
				num = AILayer.Boost(num, num3 * -0.5f);
			}
		}
		return num;
	}

	private void RefreshCityDefenseMission(ColossusMission currentColossusMission, Unit colossus)
	{
		if (currentColossusMission.Objective != ColossusMission.MissionObjective.CityDefense)
		{
			return;
		}
		WorldPosition colossusArmyPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			colossusArmyPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.CityDefense);
		float num = this.ComputeCityFitness(currentColossusMission.TargetGuid, colossusArmyPosition);
		float num2 = num * fitnessForMission;
		ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(currentColossusMission.TargetGuid);
		if (colossusMissionPerTarget != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && colossusMissionPerTarget.ColossusGuid != colossus.GUID && num2 < colossusMissionPerTarget.Fitness)
		{
			num *= 0.5f;
		}
		num = AILayer.Boost(num, 0.1f);
		currentColossusMission.EntityFitness = num;
		currentColossusMission.ColossusMissionFitness = fitnessForMission;
	}

	public ColossusMission GetColossusMissionPerColossus(GameEntityGUID colossusGuid)
	{
		for (int i = 0; i < base.AICommanders.Count; i++)
		{
			AICommander_Colossus aicommander_Colossus = base.AICommanders[i] as AICommander_Colossus;
			if (aicommander_Colossus != null && aicommander_Colossus.CurrentColossusMission != null && aicommander_Colossus.CurrentColossusMission.ColossusGuid == colossusGuid)
			{
				return aicommander_Colossus.CurrentColossusMission;
			}
		}
		return null;
	}

	public ColossusMission GetColossusMissionPerTarget(GameEntityGUID targetGuid)
	{
		for (int i = 0; i < base.AICommanders.Count; i++)
		{
			AICommander_Colossus aicommander_Colossus = base.AICommanders[i] as AICommander_Colossus;
			if (aicommander_Colossus != null && aicommander_Colossus.CurrentColossusMission != null && aicommander_Colossus.CurrentColossusMission.TargetGuid == targetGuid)
			{
				return aicommander_Colossus.CurrentColossusMission;
			}
		}
		return null;
	}

	private float GetFitnessForMission(Unit colossus, ColossusMission.MissionObjective missionObjective)
	{
		ColossusMissionDefinition colossusMissionDefinition;
		if (this.colossusMissionDatabase.TryGetValue(missionObjective.ToString(), out colossusMissionDefinition))
		{
			for (int i = 0; i < colossusMissionDefinition.UnitBodyPreference.Length; i++)
			{
				if (colossus.UnitDesign.UnitBodyDefinition.Name == colossusMissionDefinition.UnitBodyPreference[i].Name)
				{
					return colossusMissionDefinition.UnitBodyPreference[i].Value;
				}
			}
		}
		return 0f;
	}

	public ColossusMission UpdateHarassingMission(ColossusMission currentColossusMission, Unit colossus)
	{
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.Harassing);
		WorldPosition worldPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			worldPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		Region region = this.worldPositioningService.GetRegion(worldPosition);
		float num = 0f;
		GameEntityGUID targetGuid = GameEntityGUID.Zero;
		global::Game game = this.gameService.Game as global::Game;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is MajorEmpire)
			{
				if (base.AIEntity.Empire.Index != i)
				{
					DepartmentOfTheInterior agency = game.Empires[i].GetAgency<DepartmentOfTheInterior>();
					int j = 0;
					while (j < agency.Cities.Count)
					{
						if (this.hasShips)
						{
							goto IL_EF;
						}
						Region region2 = this.worldPositioningService.GetRegion(agency.Cities[j].WorldPosition);
						if (region.ContinentID == region2.ContinentID)
						{
							goto IL_EF;
						}
						IL_12B:
						j++;
						continue;
						IL_EF:
						float num2 = this.ComputeHarassingFitness(agency.Cities[j].GUID, worldPosition);
						if (num < num2)
						{
							num = num2;
							targetGuid = agency.Cities[j].GUID;
							goto IL_12B;
						}
						goto IL_12B;
					}
				}
			}
		}
		if (!targetGuid.IsValid)
		{
			return null;
		}
		float num3 = num * fitnessForMission;
		ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(targetGuid);
		if (colossusMissionPerTarget != null && currentColossusMission != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && num3 < colossusMissionPerTarget.Fitness)
		{
			num *= 0.5f;
			num3 *= 0.5f;
		}
		if (currentColossusMission != null && num3 < currentColossusMission.Fitness)
		{
			return null;
		}
		return new ColossusMission
		{
			EntityFitness = num,
			ColossusMissionFitness = fitnessForMission,
			ColossusGuid = colossus.GUID,
			TargetGuid = targetGuid,
			Objective = ColossusMission.MissionObjective.Harassing
		};
	}

	private float ComputeHarassingFitness(GameEntityGUID cityGuid, WorldPosition colossusArmyPosition)
	{
		float num = 0f;
		bool flag = true;
		AIData_City aidata_City;
		if (this.aiDataRepositoryHelper.TryGetAIData<AIData_City>(cityGuid, out aidata_City))
		{
			if (!this.departmentOfForeignAffairs.CanBesiegeCity(aidata_City.City))
			{
				flag = false;
			}
			if (flag)
			{
				AIRegionData regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, aidata_City.City.Region.Index);
				if (regionData != null)
				{
					num = regionData.HarassingScore;
					if (colossusArmyPosition.IsValid)
					{
						float num2 = (float)this.worldPositioningService.GetDistance(aidata_City.City.WorldPosition, colossusArmyPosition);
						float num3 = Mathf.Clamp01(num2 / this.averageMaximumMovementPoint / 5f);
						num = AILayer.Boost(num, num3 * -0.5f);
					}
				}
			}
		}
		return num;
	}

	private void RefreshHarassingMission(ColossusMission currentColossusMission, Unit colossus)
	{
		if (currentColossusMission.Objective != ColossusMission.MissionObjective.Harassing)
		{
			return;
		}
		WorldPosition colossusArmyPosition = WorldPosition.Invalid;
		if (colossus.Garrison is IWorldPositionable)
		{
			colossusArmyPosition = (colossus.Garrison as IWorldPositionable).WorldPosition;
		}
		float fitnessForMission = this.GetFitnessForMission(colossus, ColossusMission.MissionObjective.Harassing);
		float num = this.ComputeHarassingFitness(currentColossusMission.TargetGuid, colossusArmyPosition);
		num = AILayer.Boost(num, 0.1f);
		float num2 = num * fitnessForMission;
		ColossusMission colossusMissionPerTarget = this.GetColossusMissionPerTarget(currentColossusMission.TargetGuid);
		if (colossusMissionPerTarget != null && colossusMissionPerTarget.Objective == currentColossusMission.Objective && colossusMissionPerTarget.ColossusGuid != colossus.GUID && num2 < colossusMissionPerTarget.Fitness)
		{
			num *= 0.5f;
		}
		currentColossusMission.EntityFitness = num;
		currentColossusMission.ColossusMissionFitness = fitnessForMission;
	}

	public override IEnumerator Initialize(AIEntity aiEntity)
	{
		yield return base.Initialize(aiEntity);
		this.colossusTags.AddTag(DownloadableContent9.TagColossus);
		this.departmentOfDefense = base.AIEntity.Empire.GetAgency<DepartmentOfDefense>();
		this.departmentOfTheTreasury = base.AIEntity.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.departmentOfTheInterior = base.AIEntity.Empire.GetAgency<DepartmentOfTheInterior>();
		this.departmentOfForeignAffairs = base.AIEntity.Empire.GetAgency<DepartmentOfForeignAffairs>();
		this.unitDesignDataRepository = AIScheduler.Services.GetService<IAIUnitDesignDataRepository>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.worldAtlasAIHelper = AIScheduler.Services.GetService<IWorldAtlasAIHelper>();
		this.empireDataAIHelper = AIScheduler.Services.GetService<IAIEmpireDataAIHelper>();
		this.endTurnService = Services.GetService<IEndTurnService>();
		this.gameService = Services.GetService<IGameService>();
		this.worldPositioningService = this.gameService.Game.Services.GetService<IWorldPositionningService>();
		this.colossusMissionDatabase = Databases.GetDatabase<ColossusMissionDefinition>(false);
		this.updateColossusMissions.Add(new Func<ColossusMission, Unit, ColossusMission>(this.UpdateArmySupportMission));
		this.updateColossusMissions.Add(new Func<ColossusMission, Unit, ColossusMission>(this.UpdateHarassingMission));
		this.updateColossusMissions.Add(new Func<ColossusMission, Unit, ColossusMission>(this.UpdateCityDefenseMission));
		this.refreshColossusMissions.Add(new Action<ColossusMission, Unit>(this.RefreshArmySupportMission));
		this.refreshColossusMissions.Add(new Action<ColossusMission, Unit>(this.RefreshHarassingMission));
		this.refreshColossusMissions.Add(new Action<ColossusMission, Unit>(this.RefreshCityDefenseMission));
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService.IsShared(DownloadableContent9.ReadOnlyName))
		{
			base.AIEntity.RegisterPass(AIEntity.Passes.RefreshObjectives.ToString(), "AILayer_Colossus_RefreshObjectives", new AIEntity.AIAction(this.RefreshObjectives), this, new StaticString[0]);
			base.AIEntity.RegisterPass(AIEntity.Passes.ExecuteNeeds.ToString(), "AILayer_Colossus_ExecuteNeeds", new AIEntity.AIAction(this.ExecuteNeeds), this, new StaticString[0]);
		}
		yield break;
	}

	public override bool IsActive()
	{
		return base.AIEntity.AIPlayer.AIState == AIPlayer.PlayerState.EmpireControlledByAI;
	}

	public override void Release()
	{
		base.Release();
		this.colossusTags = null;
		this.departmentOfDefense = null;
		this.departmentOfTheTreasury = null;
		this.departmentOfTheInterior = null;
		this.departmentOfForeignAffairs = null;
		this.unitDesignDataRepository = null;
		this.aiDataRepositoryHelper = null;
		this.worldAtlasAIHelper = null;
		this.empireDataAIHelper = null;
		this.endTurnService = null;
		this.gameService = null;
		this.worldPositioningService = null;
		this.colossusMissionDatabase = null;
		this.updateColossusMissions.Clear();
		this.refreshColossusMissions.Clear();
	}

	protected override void ExecuteNeeds(StaticString context, StaticString pass)
	{
		base.ExecuteNeeds(context, pass);
		this.colossusData.Clear();
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && aidata.IsColossus)
			{
				this.colossusData.Add(aidata);
			}
		}
		int index;
		for (index = 0; index < this.colossusData.Count; index++)
		{
			if (!this.aiCommanders.Exists((AICommander match) => match.ForceArmyGUID == this.colossusData[index].Army.GUID))
			{
				this.AddCommander(new AICommander_Colossus
				{
					ForceArmyGUID = this.colossusData[index].Army.GUID,
					Empire = base.AIEntity.Empire,
					AIPlayer = base.AIEntity.AIPlayer
				});
			}
		}
		this.UpdateColossusMissions();
	}

	protected override void RefreshObjectives(StaticString context, StaticString pass)
	{
		base.RefreshObjectives(context, pass);
		this.averageMaximumMovementPoint = 4f;
		AIEmpireData aiempireData;
		if (this.empireDataAIHelper.TryGet(base.AIEntity.Empire.Index, out aiempireData) && aiempireData.AverageUnitDesignMaximumMovement > 0f)
		{
			this.averageMaximumMovementPoint = aiempireData.AverageUnitDesignMaximumMovement;
			this.hasShips = aiempireData.HasShips;
		}
		this.numberOfLivingMajorEmpire = 0;
		global::Game game = this.gameService.Game as global::Game;
		for (int i = 0; i < game.Empires.Length; i++)
		{
			if (game.Empires[i] is MajorEmpire)
			{
				MajorEmpire majorEmpire = game.Empires[i] as MajorEmpire;
				if (!majorEmpire.IsEliminated)
				{
					this.numberOfLivingMajorEmpire++;
				}
				if (i != base.AIEntity.Empire.Index)
				{
					this.UpdateHarassingScore(majorEmpire);
				}
			}
		}
		this.UpdateCitiesDefenseScore();
		this.UpdateArmiesSupportScore();
		this.canCreateAnotherColossus = false;
		this.buildableColossusUnitDesignData.Clear();
		this.colossusUnitDesignData.Clear();
		this.unitDesignDataRepository.FillUnitDesignByTags(base.AIEntity.Empire.Index, ref this.colossusUnitDesignData, this.colossusTags);
		for (int j = 0; j < this.colossusUnitDesignData.Count; j++)
		{
			UnitDesign unitDesign;
			if (this.departmentOfDefense.UnitDesignDatabase.TryGetValue(this.colossusUnitDesignData[j].UnitDesignModel, out unitDesign, false) && DepartmentOfTheTreasury.CheckConstructiblePrerequisites(base.AIEntity.Empire, unitDesign, new string[]
			{
				ConstructionFlags.AIColossusPrerequisite
			}))
			{
				this.canCreateAnotherColossus = true;
				break;
			}
		}
		if (!this.canCreateAnotherColossus)
		{
			return;
		}
		for (int k = this.colossusUnitDesignData.Count - 1; k >= 0; k--)
		{
			UnitDesign unitDesign;
			if (this.departmentOfDefense.UnitDesignDatabase.TryGetValue(this.colossusUnitDesignData[k].UnitDesignModel, out unitDesign, false))
			{
				this.missingResources = this.departmentOfTheTreasury.GetConstructibleMissingRessources(base.AIEntity.Empire, unitDesign);
				if (this.missingResources != null && this.missingResources.Count > 0)
				{
					float num = 0f;
					for (int l = 0; l < this.missingResources.Count; l++)
					{
						num += this.missingResources[l].MissingResourceValue / this.missingResources[l].AskedResourceValue;
					}
					float num2 = 0f;
					for (int m = 0; m < unitDesign.Costs.Length; m++)
					{
						if (unitDesign.Costs[m].Instant)
						{
							num2 += 1f;
						}
					}
					num /= num2;
					if (num >= 0.4f)
					{
						goto IL_30E;
					}
				}
				this.buildableColossusUnitDesignData.Add(this.colossusUnitDesignData[k]);
			}
			IL_30E:;
		}
		if (this.buildableColossusUnitDesignData.Count == 0)
		{
			return;
		}
		float num3 = 0f;
		UnitDesign unitDesign2 = null;
		for (int n = 0; n < this.buildableColossusUnitDesignData.Count; n++)
		{
			UnitDesign unitDesign;
			if (this.departmentOfDefense.UnitDesignDatabase.TryGetValue(this.buildableColossusUnitDesignData[n].UnitDesignModel, out unitDesign, false))
			{
				float num4 = this.ComputeColossusScore(this.buildableColossusUnitDesignData[n], unitDesign);
				if (num4 > num3)
				{
					num3 = num4;
					unitDesign2 = unitDesign;
				}
			}
		}
		if (unitDesign2 == null)
		{
			return;
		}
		EvaluableMessage_Colossus evaluableMessage_Colossus = base.AIEntity.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Colossus>(BlackboardLayerID.Empire, (EvaluableMessage_Colossus match) => match.State != BlackboardMessage.StateValue.Message_Canceled || match.State != BlackboardMessage.StateValue.Message_Failed);
		if (evaluableMessage_Colossus != null)
		{
			if (evaluableMessage_Colossus.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending || evaluableMessage_Colossus.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending_MissingResource || evaluableMessage_Colossus.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate)
			{
				evaluableMessage_Colossus.ResetState();
				evaluableMessage_Colossus.ResetUnitDesign(unitDesign2);
			}
			else if (evaluableMessage_Colossus.EvaluationState != EvaluableMessage.EvaluableMessageState.Obtaining)
			{
				evaluableMessage_Colossus = null;
			}
		}
		if (evaluableMessage_Colossus == null)
		{
			evaluableMessage_Colossus = new EvaluableMessage_Colossus(new HeuristicValue(0f), new HeuristicValue(0f), unitDesign2, -1, 1, AILayer_AccountManager.MilitaryAccountName);
			base.AIEntity.AIPlayer.Blackboard.AddMessage(evaluableMessage_Colossus);
		}
		float globalMotivation = this.ComputeGlobalPriority();
		float localOpportunity = this.ComputeLocalPriority();
		evaluableMessage_Colossus.Refresh(globalMotivation, localOpportunity);
		evaluableMessage_Colossus.TimeOut = 1;
	}

	private float ComputeColossusScore(AIUnitDesignData unitDesignData, UnitDesign unitDesign)
	{
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < unitDesign.Costs.Length; i++)
		{
			if (unitDesign.Costs[i].Instant)
			{
				num2 += 1f;
				float num3;
				if (this.departmentOfTheTreasury.TryGetNetResourceValue(base.AIEntity.Empire, unitDesign.Costs[i].ResourceName, out num3, false))
				{
					num += 1f;
				}
			}
		}
		float num4 = num / num2;
		return num4 / (unitDesignData.EmpireUnitCount + 1f);
	}

	private float ComputeGlobalPriority()
	{
		float propertyValue = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.MaximumNumberOfColossi);
		float propertyValue2 = base.AIEntity.Empire.GetPropertyValue(SimulationProperties.NumberOfColossi);
		float num = 0f;
		if (propertyValue > 0f)
		{
			num = (propertyValue - propertyValue2) / propertyValue;
		}
		float propertyValue3 = base.AIEntity.Empire.GetPropertyValue("CurrentEra");
		float num2 = 6f;
		float num3 = propertyValue3 / num2;
		AILayer_War layer = base.AIEntity.GetLayer<AILayer_War>();
		float num4 = (float)Mathf.Max(1, layer.NumberOfWantedWar + layer.NumberOfWar);
		float num5 = 0f;
		if (this.numberOfLivingMajorEmpire > 1)
		{
			num5 = num4 / (float)(this.numberOfLivingMajorEmpire - 1);
		}
		return (num + num3 + num5) / 3f;
	}

	private float ComputeLocalPriority()
	{
		return 0.8f;
	}

	private void UpdateArmiesSupportScore()
	{
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			AIData_Army aidata_Army;
			if (this.aiDataRepositoryHelper.TryGetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID, out aidata_Army))
			{
				if (aidata_Army.IsColossus)
				{
					aidata_Army.SupportScore = -1f;
				}
				else
				{
					float num = 0.5f;
					float num2 = 0f;
					if (aidata_Army.CommanderMission != null)
					{
						num2 = aidata_Army.CommanderMission.Commander.GetPriority(aidata_Army.CommanderMission);
					}
					num = AILayer.Boost(num, 0.5f * num2);
					aidata_Army.SupportScore = num;
				}
			}
		}
	}

	private void UpdateCitiesDefenseScore()
	{
		for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
		{
			City city = this.departmentOfTheInterior.Cities[i];
			float num = 0.5f;
			float num2 = city.GetPropertyValue(SimulationProperties.CityDefensePoint) / city.GetPropertyValue(SimulationProperties.MaximumCityDefensePoint);
			float num3 = 0f;
			num = AILayer.Boost(num, -0.2f * num2);
			num = AILayer.Boost(num, -0.2f * num3);
			int num4 = this.endTurnService.Turn - 10;
			int num5 = 0;
			AIRegionData regionData;
			for (int j = 0; j < city.Region.Borders.Length; j++)
			{
				regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, city.Region.Borders[j].NeighbourRegionIndex);
				if (regionData != null && regionData.LostByMeAtTurn > num4)
				{
					num5++;
				}
			}
			regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, city.Region.Index);
			float num6;
			float num7;
			if (num5 > 0)
			{
				num6 = 0.5f;
				num7 = 0.8f + (float)num5 * 0.05f;
			}
			else if (regionData.BorderWithEnnemy > 0)
			{
				num6 = 0.3f;
				num7 = 0.8f + (float)regionData.BorderWithEnnemy * 0.05f;
			}
			else
			{
				num6 = -0.2f;
				num7 = 1f;
			}
			num = AILayer.Boost(num, num6 * num7);
			AIData_City aidata = this.aiDataRepositoryHelper.GetAIData<AIData_City>(this.departmentOfTheInterior.Cities[i].GUID);
			aidata.DefenseScore = num;
		}
	}

	private void UpdateColossusMissions()
	{
		for (int i = 0; i < this.colossusData.Count; i++)
		{
			Unit unit = this.colossusData[i].Army.StandardUnits[0];
			ColossusMission colossusMissionPerColossus = this.GetColossusMissionPerColossus(unit.GUID);
			if (colossusMissionPerColossus != null)
			{
				for (int j = 0; j < this.refreshColossusMissions.Count; j++)
				{
					this.refreshColossusMissions[j](colossusMissionPerColossus, unit);
				}
			}
		}
		for (int k = 0; k < this.colossusData.Count; k++)
		{
			Unit unit2 = this.colossusData[k].Army.StandardUnits[0];
			ColossusMission colossusMission = this.GetColossusMissionPerColossus(unit2.GUID);
			bool flag = false;
			for (int l = 0; l < this.refreshColossusMissions.Count; l++)
			{
				ColossusMission colossusMission2 = this.updateColossusMissions[l](colossusMission, unit2);
				if (colossusMission2 != null)
				{
					flag = true;
					colossusMission = colossusMission2;
				}
			}
			if (flag)
			{
				for (int m = 0; m < base.AICommanders.Count; m++)
				{
					if (base.AICommanders[m].ForceArmyGUID == this.colossusData[k].Army.GUID)
					{
						AICommander_Colossus aicommander_Colossus = this.aiCommanders[k] as AICommander_Colossus;
						aicommander_Colossus.ChangeColossusMission(colossusMission);
					}
				}
			}
		}
	}

	private void UpdateHarassingScore(global::Empire empire)
	{
		DepartmentOfTheInterior agency = empire.GetAgency<DepartmentOfTheInterior>();
		if (!this.departmentOfForeignAffairs.IsEnnemy(empire))
		{
			return;
		}
		float num = 0.5f;
		if (this.departmentOfForeignAffairs.IsAtWarWith(empire))
		{
			num = 1f;
		}
		for (int i = 0; i < agency.Cities.Count; i++)
		{
			AIRegionData regionData = this.worldAtlasAIHelper.GetRegionData(base.AIEntity.Empire.Index, agency.Cities[i].Region.Index);
			float num2 = regionData.MinimalDistanceToMyCities / this.averageMaximumMovementPoint;
			float num3 = (0.5f - Mathf.Clamp01(num2 / 10f)) / 0.5f;
			int num4 = regionData.WatchTowerPointOfInterestCount + regionData.ResourcePointOfInterestCount;
			float num5 = 0f;
			if (num4 > 0)
			{
				num5 = (float)(regionData.BuiltWatchTower + regionData.BuiltExtractor) / (float)num4;
			}
			float num6 = 0f;
			float num7 = 10f;
			if ((float)regionData.LostByMeAtTurn > (float)this.endTurnService.Turn - num7)
			{
				num6 = 1f - (float)(this.endTurnService.Turn - regionData.LostByMeAtTurn) / num7;
			}
			float num8 = 0f;
			num8 = AILayer.Boost(num8, num5 * 0.2f);
			num8 = AILayer.Boost(num8, num6 * 0.3f);
			num8 = AILayer.Boost(num8, num3 * 0.5f);
			num8 = AILayer.Boost(num8, num * 0.5f);
			regionData.HarassingScore = num8;
		}
	}

	private IDatabase<ColossusMissionDefinition> colossusMissionDatabase;

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private float averageMaximumMovementPoint;

	private List<AIUnitDesignData> buildableColossusUnitDesignData = new List<AIUnitDesignData>();

	private bool canCreateAnotherColossus;

	private List<AIData_Army> colossusData = new List<AIData_Army>();

	private Tags colossusTags = new Tags();

	private List<AIUnitDesignData> colossusUnitDesignData = new List<AIUnitDesignData>();

	private DepartmentOfDefense departmentOfDefense;

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;

	private DepartmentOfTheInterior departmentOfTheInterior;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private IAIEmpireDataAIHelper empireDataAIHelper;

	private IEndTurnService endTurnService;

	private IGameService gameService;

	private List<MissingResource> missingResources = new List<MissingResource>();

	private int numberOfLivingMajorEmpire;

	private List<Action<ColossusMission, Unit>> refreshColossusMissions = new List<Action<ColossusMission, Unit>>();

	private IAIUnitDesignDataRepository unitDesignDataRepository;

	private List<Func<ColossusMission, Unit, ColossusMission>> updateColossusMissions = new List<Func<ColossusMission, Unit, ColossusMission>>();

	private IWorldAtlasAIHelper worldAtlasAIHelper;

	private IWorldPositionningService worldPositioningService;

	private bool hasShips;
}
