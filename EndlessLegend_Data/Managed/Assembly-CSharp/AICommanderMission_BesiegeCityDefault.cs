﻿using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_BesiegeCityDefault : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_BesiegeCityDefault()
	{
		this.TargetCity = null;
		base.SeasonToSwitchTo = Season.ReadOnlyWinter;
	}

	public override void ReadXml(XmlReader reader)
	{
		GameEntityGUID guid = reader.GetAttribute<ulong>("TargetGUID");
		if (guid.IsValid)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			IGameEntityRepositoryService service2 = service.Game.Services.GetService<IGameEntityRepositoryService>();
			Diagnostics.Assert(service2 != null);
			IGameEntity gameEntity;
			service2.TryGetValue(guid, out gameEntity);
			this.TargetCity = (gameEntity as City);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("TargetGUID", (this.TargetCity != null) ? this.TargetCity.GUID : GameEntityGUID.Zero);
		base.WriteXml(writer);
	}

	public bool IsReinforcement { get; set; }

	public bool MayAttack { get; set; }

	public City TargetCity { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.TargetCity != null)
		{
			return this.TargetCity.GetValidDistrictToTarget(null).WorldPosition;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		if (commander.Empire is MajorEmpire)
		{
			this.departmentOfForeignAffairs = commander.Empire.GetAgency<DepartmentOfForeignAffairs>();
		}
	}

	public override void Release()
	{
		base.Release();
		this.TargetCity = null;
		this.departmentOfForeignAffairs = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.TargetCity = (parameters[0] as City);
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Commander.Empire, this.TargetCity, 0);
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		return this.TargetCity == null || this.TargetCity.Empire == base.Commander.Empire || (this.departmentOfForeignAffairs != null && !this.departmentOfForeignAffairs.CanBesiegeCity(this.TargetCity)) || this.aiDataRepository.GetAIData<AIData_City>(this.TargetCity.GUID) == null;
	}

	protected override void Pending()
	{
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Running;
		this.State = TickableState.NoTick;
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

	protected override bool TryComputeArmyMissionParameter()
	{
		base.ArmyMissionParameters.Clear();
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null)
		{
			return false;
		}
		City targetCity = this.TargetCity;
		return base.TryCreateArmyMission("BesiegeCity", new List<object>
		{
			targetCity,
			this.MayAttack
		});
	}

	protected override void Success()
	{
		base.Success();
		if (this.TargetCity == null || this.TargetCity.Empire == base.Commander.Empire || (this.departmentOfForeignAffairs != null && !this.departmentOfForeignAffairs.CanBesiegeCity(this.TargetCity)))
		{
			base.SetArmyFree();
		}
		if (this.aiDataRepository.GetAIData<AIData_City>(this.TargetCity.GUID) == null)
		{
			base.SetArmyFree();
		}
	}

	private DepartmentOfForeignAffairs departmentOfForeignAffairs;
}
