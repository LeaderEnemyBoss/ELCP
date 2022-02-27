using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_Pacification : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_Pacification()
	{
		this.RegionTarget = null;
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		base.WriteXml(writer);
	}

	public Region RegionTarget { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		Army maxHostileArmy = AILayer_Pacification.GetMaxHostileArmy(base.Commander.Empire, this.RegionTarget.Index);
		if (maxHostileArmy != null)
		{
			return maxHostileArmy.WorldPosition;
		}
		if (this.RegionTarget != null)
		{
			return this.RegionTarget.Barycenter;
		}
		return WorldPosition.Invalid;
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionTarget == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
	}

	protected override void ArmyLost()
	{
		base.ArmyLost();
		if (this.IsMissionCompleted())
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		}
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
		minMilitaryPower = Intelligence.EvaluateAllFactionMaxMilitaryPowerOnRegion(base.Commander.Empire, this.RegionTarget.Index, true);
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		return this.RegionTarget == null || (this.RegionTarget.City != null && this.RegionTarget.City.Empire != base.Commander.Empire) || !AILayer_Pacification.RegionContainsHostileArmies(base.Commander.Empire, this.RegionTarget.Index);
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
		if (this.RegionTarget == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		base.ArmyMissionParameters.Clear();
		if (base.AIDataArmyGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		Army maxHostileArmy = AILayer_Pacification.GetMaxHostileArmy(base.Commander.Empire, this.RegionTarget.Index);
		if (maxHostileArmy != null && aidata.Army.GetPropertyValue(SimulationProperties.MilitaryPower) > 0.8f * maxHostileArmy.GetPropertyValue(SimulationProperties.MilitaryPower))
		{
			return base.TryCreateArmyMission("MajorFactionAttackArmy", new List<object>
			{
				maxHostileArmy,
				this.RegionTarget.Index
			});
		}
		return base.TryCreateArmyMission("MajorFactionRoaming", new List<object>
		{
			this.RegionTarget.Index
		});
	}
}
