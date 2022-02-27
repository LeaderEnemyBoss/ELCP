using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_AttackCampDefault : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_AttackCampDefault()
	{
		this.TargetCamp = null;
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
			this.TargetCamp = (gameEntity as Camp);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("TargetGUID", (this.TargetCamp != null) ? this.TargetCamp.GUID : GameEntityGUID.Zero);
		base.WriteXml(writer);
	}

	public bool IsReinforcement { get; set; }

	public Camp TargetCamp { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.TargetCamp != null)
		{
			return this.TargetCamp.GetValidDistrictToTarget(null).WorldPosition;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
	}

	public override void Release()
	{
		base.Release();
		this.TargetCamp = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.TargetCamp = (parameters[0] as Camp);
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Commander.Empire, this.TargetCamp, 0);
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		if (this.TargetCamp == null || this.TargetCamp.MainDistrict == null || this.TargetCamp.Empire == base.Commander.Empire)
		{
			return true;
		}
		AIData_City aidata = this.aiDataRepository.GetAIData<AIData_City>(this.TargetCamp.City.GUID);
		return aidata == null;
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
		Camp targetCamp = this.TargetCamp;
		return base.TryCreateArmyMission("AttackCamp", new List<object>
		{
			targetCamp
		});
	}
}
