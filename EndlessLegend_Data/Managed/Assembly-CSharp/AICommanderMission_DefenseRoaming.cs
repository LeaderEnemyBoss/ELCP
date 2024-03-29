﻿using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Session;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_DefenseRoaming : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_DefenseRoaming()
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
			World world = (service.Game as global::Game).World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		this.IsWarBased = false;
		this.IsWarBased = reader.GetAttribute<bool>("IsWarBased");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<bool>("IsWarBased", this.IsWarBased);
		base.WriteXml(writer);
	}

	public bool IsWarBased { get; set; }

	public Region RegionTarget { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
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

	public override void Promote()
	{
		AIScheduler.Services.GetService<ITickableRepositoryAIHelper>().Register(this);
		base.IsActive = true;
		base.State = TickableState.NeedTick;
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
		this.ailayer_War = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
		this.IsWarBased = (bool)parameters[1];
	}

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = 1f;
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	protected override bool IsMissionCompleted()
	{
		if (base.Commander.Empire != null && base.Commander.Empire is MajorEmpire && this.ailayer_QuestSolver == null)
		{
			GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
			try
			{
				AIPlayer_MajorEmpire aiplayer_MajorEmpire;
				if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.Commander.Empire as MajorEmpire, out aiplayer_MajorEmpire))
				{
					AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
					if (entity != null)
					{
						this.ailayer_QuestSolver = entity.GetLayer<AILayer_QuestSolver>();
					}
				}
			}
			catch (Exception ex)
			{
				Diagnostics.LogError("Exceptions caught: {0}", new object[]
				{
					ex
				});
				return false;
			}
		}
		return !(base.Commander is AICommander_Victory) && !AILayer_Patrol.IsPatrolValid(base.Commander.Empire, this.RegionTarget, this.ailayer_QuestSolver);
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
			return false;
		}
		Diagnostics.Assert(aidata != null);
		Diagnostics.Assert(aidata.Army != null);
		List<object> list = new List<object>();
		list.Add(this.RegionTarget.Index);
		list.Add(this.IsWarBased);
		if (base.Commander.Empire != null && base.Commander.Empire is MajorEmpire)
		{
			if (this.ailayer_War == null)
			{
				GameServer gameServer = (Services.GetService<ISessionService>().Session as global::Session).GameServer as GameServer;
				AIPlayer_MajorEmpire aiplayer_MajorEmpire;
				if (gameServer.AIScheduler != null && gameServer.AIScheduler.TryGetMajorEmpireAIPlayer(base.Commander.Empire as MajorEmpire, out aiplayer_MajorEmpire))
				{
					AIEntity entity = aiplayer_MajorEmpire.GetEntity<AIEntity_Empire>();
					if (entity != null)
					{
						this.ailayer_War = entity.GetLayer<AILayer_War>();
					}
				}
			}
			if (this.ailayer_War != null)
			{
				this.ailayer_War.AssignDefensiveArmyToCity(aidata.Army);
			}
		}
		if (this.IsWarBased)
		{
			if (base.TryCreateArmyMission("MajorFactionWarRoaming", list))
			{
				return true;
			}
		}
		else if (base.TryCreateArmyMission("MajorFactionRoaming", list))
		{
			return true;
		}
		return false;
	}

	private AILayer_War ailayer_War;

	private AILayer_QuestSolver ailayer_QuestSolver;
}
