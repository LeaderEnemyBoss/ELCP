﻿using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_Village : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_Village()
	{
		this.RegionTarget = null;
		this.Village = null;
		base.SeasonToSwitchTo = Season.ReadOnlyWinter;
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			World world = (service.Game as global::Game).World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		this.VillageGUID = reader.GetAttribute<ulong>("VillageGUID");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<ulong>("VillageGUID", this.VillageGUID);
		base.WriteXml(writer);
	}

	public Region RegionTarget { get; set; }

	public Village Village { get; set; }

	public GameEntityGUID VillageGUID { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.Village == null)
		{
			this.Village = this.SelectVillage();
		}
		if (this.Village != null)
		{
			return this.Village.WorldPosition;
		}
		if (this.RegionTarget != null)
		{
			return this.RegionTarget.Barycenter;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionTarget == null || this.RegionTarget.MinorEmpire == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		this.barbarianCouncil = this.RegionTarget.MinorEmpire.GetAgency<BarbarianCouncil>();
		this.Village = this.SelectVillage();
	}

	public override void Release()
	{
		base.Release();
		this.barbarianCouncil = null;
		this.RegionTarget = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
		this.VillageGUID = (GameEntityGUID)parameters[1];
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army armyData, out TickableState tickableState)
	{
		tickableState = TickableState.Optional;
		if (this.IsMissionCompleted())
		{
			return AICommanderMission.AICommanderMissionCompletion.Success;
		}
		return AICommanderMission.AICommanderMissionCompletion.Initializing;
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = this.intelligenceAIHelper.EvaluateMaxMilitaryPowerOfRegion(base.Commander.Empire, this.RegionTarget.Index);
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		return this.Village != null && ((this.Village.HasBeenConverted && this.Village.Converter == base.Commander.Empire) || (this.Village.HasBeenPacified && this.Village.PointOfInterest.PointOfInterestImprovement == null));
	}

	protected override void Running()
	{
		if (this.IsMissionCompleted())
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
	}

	protected override void Success()
	{
		base.Success();
		base.SetArmyFree();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		bool result;
		if (this.RegionTarget == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			result = false;
		}
		else
		{
			base.ArmyMissionParameters.Clear();
			if (base.AIDataArmyGUID == GameEntityGUID.Zero)
			{
				result = false;
			}
			else
			{
				this.Village = this.SelectVillage();
				if (this.Village == null)
				{
					base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
					result = true;
				}
				else
				{
					List<object> list = new List<object>();
					list.Add(this.RegionTarget.Index);
					list.Add(this.Village);
					AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
					if (aidata != null)
					{
						Army army = aidata.Army;
						if (army != null && !DepartmentOfTheInterior.IsArmyAbleToConvert(army, true) && base.TryCreateArmyMission("PacifyVillage", new List<object>
						{
							this.Village
						}))
						{
							return true;
						}
					}
					if (this.Village.HasBeenConverted && this.Village.Converter != base.Commander.Empire as MajorEmpire)
					{
						result = base.TryCreateArmyMission("PacifyVillage", new List<object>
						{
							this.Village
						});
					}
					else
					{
						if (this.Village.HasBeenPacified)
						{
							if (base.TryCreateArmyMission("ConvertVillage", list))
							{
								return true;
							}
						}
						else if (base.TryCreateArmyMission("PacifyAndConvertVillage", list))
						{
							return true;
						}
						result = false;
					}
				}
			}
		}
		return result;
	}

	private Village SelectVillage()
	{
		for (int i = 0; i < this.barbarianCouncil.Villages.Count; i++)
		{
			if (this.barbarianCouncil.Villages[i].GUID == this.VillageGUID)
			{
				return this.barbarianCouncil.Villages[i];
			}
		}
		return null;
	}

	private BarbarianCouncil barbarianCouncil;
}
