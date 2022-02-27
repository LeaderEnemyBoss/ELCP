using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_SiegeBreaker : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_SiegeBreaker()
	{
		this.RegionWithCityToFree = null;
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionWithCityToFree = null;
		if (attribute > -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.RegionWithCityToFree = world.Regions[attribute];
			Diagnostics.Assert(this.RegionWithCityToFree != null);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionWithCityToFree != null) ? this.RegionWithCityToFree.Index : -1);
		base.WriteXml(writer);
	}

	public Region RegionWithCityToFree { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.RegionWithCityToFree == null)
		{
			return WorldPosition.Invalid;
		}
		if (this.RegionWithCityToFree.City != null)
		{
			return this.RegionWithCityToFree.City.GetValidDistrictToTarget(null).WorldPosition;
		}
		return this.RegionWithCityToFree.Barycenter;
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionWithCityToFree == null || this.RegionWithCityToFree.City == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
	}

	public override void Release()
	{
		base.Release();
		this.RegionWithCityToFree = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionWithCityToFree = (parameters[0] as Region);
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
		minMilitaryPower = this.intelligenceAIHelper.EvaluateMilitaryPowerOfBesieger(base.Commander.Empire, this.RegionWithCityToFree.Index);
		if (this.RegionWithCityToFree != null && this.RegionWithCityToFree.City != null)
		{
			float num = this.intelligenceAIHelper.EvaluateMilitaryPowerOfGarrison(base.Commander.Empire, this.RegionWithCityToFree.City, 0);
			minMilitaryPower -= num;
		}
	}

	protected override int GetNeededAvailabilityTime()
	{
		float num = -DepartmentOfTheInterior.GetBesiegingPower(this.RegionWithCityToFree.City, true);
		float propertyValue = this.RegionWithCityToFree.City.GetPropertyValue(SimulationProperties.CityDefensePoint);
		return (int)Math.Floor((double)(propertyValue / num));
	}

	protected override bool IsMissionCompleted()
	{
		return this.RegionWithCityToFree == null || this.RegionWithCityToFree.City == null || this.RegionWithCityToFree.City.Empire != base.Commander.Empire || this.RegionWithCityToFree.City.BesiegingEmpire == null;
	}

	protected override void Success()
	{
		base.Success();
		base.SetArmyFree();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (this.RegionWithCityToFree == null || this.RegionWithCityToFree.City == null || this.RegionWithCityToFree.City.Empire != base.Commander.Empire)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		base.ArmyMissionParameters.Clear();
		return !(base.AIDataArmyGUID == GameEntityGUID.Zero) && base.TryCreateArmyMission("FreeCity", new List<object>
		{
			this.RegionWithCityToFree.City
		});
	}

	protected override void Running()
	{
		base.Running();
		if (this.IsMissionCompleted())
		{
			this.Success();
		}
	}
}
