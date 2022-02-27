using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_PrivateersHarass : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_PrivateersHarass()
	{
		this.TargetCity = null;
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
			Diagnostics.Assert(this.TargetCity != null);
		}
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("TargetGUID", (this.TargetCity != null) ? this.TargetCity.GUID : GameEntityGUID.Zero);
		base.WriteXml(writer);
	}

	public bool MayAttack { get; set; }

	public City TargetCity { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.nearestCity == null || this.nearestCity.Empire != base.Commander.Empire)
		{
			this.nearestCity = this.GetNearestCity();
		}
		if (this.nearestCity != null)
		{
			return this.nearestCity.WorldPosition;
		}
		if (this.departmentOfTheInterior != null && this.departmentOfTheInterior.Cities.Count > 0)
		{
			return this.departmentOfTheInterior.Cities[0].WorldPosition;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositionningService != null);
	}

	public override void Release()
	{
		base.Release();
		this.TargetCity = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.TargetCity = (parameters[0] as City);
		this.nearestCity = this.GetNearestCity();
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
		return this.TargetCity == null || this.TargetCity.Empire == base.Commander.Empire || this.aiDataRepository.GetAIData<AIData_City>(this.TargetCity.GUID) == null;
	}

	protected override bool MissionCanAcceptHero()
	{
		return false;
	}

	protected override void Pending()
	{
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Running;
		base.State = TickableState.NoTick;
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
		if (aidata.Army.IsPrivateers)
		{
			if (base.TryCreateArmyMission("BesiegeCity", new List<object>
			{
				this.TargetCity,
				this.MayAttack
			}))
			{
				return true;
			}
		}
		else if (base.TryCreateArmyMission("ConvertToPrivateers", new List<object>
		{
			this.TargetCity
		}))
		{
			return true;
		}
		return false;
	}

	private City GetNearestCity()
	{
		City result = null;
		if (this.departmentOfTheInterior != null)
		{
			int num = int.MaxValue;
			for (int i = 0; i < this.departmentOfTheInterior.Cities.Count; i++)
			{
				int distance = this.worldPositionningService.GetDistance(this.departmentOfTheInterior.Cities[i].WorldPosition, this.TargetCity.WorldPosition);
				if (distance < num)
				{
					num = distance;
					result = this.departmentOfTheInterior.Cities[i];
				}
			}
		}
		return result;
	}

	public override void SetExtraArmyRequestInformation()
	{
		if (this.requestArmy != null)
		{
			this.requestArmy.OnlyMercenaries = true;
		}
	}

	private City nearestCity;

	private IWorldPositionningService worldPositionningService;
}
