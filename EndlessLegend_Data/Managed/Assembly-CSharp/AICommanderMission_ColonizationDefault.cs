using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_ColonizationDefault : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_ColonizationDefault()
	{
		this.RegionTarget = null;
		this.ListOfPosition = null;
	}

	public override void ReadXml(XmlReader reader)
	{
		this.PositionIndex = reader.GetAttribute<int>("PositionIndex");
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
		int attribute2 = reader.GetAttribute<int>("Count");
		if (attribute2 > 0)
		{
			this.ListOfPosition = new WorldPosition[attribute2];
			reader.ReadStartElement("ListOfPosition");
			try
			{
				for (int i = 0; i < attribute2; i++)
				{
					this.ListOfPosition[i] = reader.ReadElementSerializable<WorldPosition>();
				}
			}
			catch
			{
				throw;
			}
			reader.ReadEndElement();
		}
		else
		{
			reader.Skip();
		}
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<int>("PositionIndex", this.PositionIndex);
		base.WriteXml(writer);
		writer.WriteStartElement("ListOfPosition");
		writer.WriteAttributeString<int>("Count", (this.ListOfPosition != null) ? this.ListOfPosition.Length : 0);
		for (int i = 0; i < this.ListOfPosition.Length; i++)
		{
			IXmlSerializable xmlSerializable = this.ListOfPosition[i];
			writer.WriteElementSerializable<IXmlSerializable>(ref xmlSerializable);
		}
		writer.WriteEndElement();
	}

	public WorldPosition[] ListOfPosition { get; set; }

	public int PositionIndex { get; set; }

	public Region RegionTarget { get; set; }

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		if (this.ListOfPosition != null && this.ListOfPosition.Length > 0)
		{
			return this.ListOfPosition[0];
		}
		if (this.RegionTarget != null)
		{
			return this.RegionTarget.Barycenter;
		}
		return WorldPosition.Invalid;
	}

	public override void Initialize(AICommander aiCommander)
	{
		base.Initialize(aiCommander);
	}

	public bool IsRegionColonized()
	{
		return this.RegionTarget.IsRegionColonized() && this.RegionTarget.Owner != null && this.RegionTarget.Owner != base.Commander.Empire;
	}

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		if (parameters.Length != 2)
		{
			Diagnostics.LogError("[AICommanderMission_ColonizationDefault] Wrong number of parameters {0}", new object[]
			{
				parameters.Length
			});
		}
		this.RegionTarget = (parameters[0] as Region);
		this.ListOfPosition = (parameters[1] as WorldPosition[]);
	}

	protected override void ArmyLost()
	{
		if (this.IsMissionCompleted())
		{
			this.State = TickableState.NoTick;
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
		}
		else
		{
			base.AIDataArmyGUID = GameEntityGUID.Zero;
			this.State = TickableState.NoTick;
			this.Interrupt();
		}
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionFor(AIArmyMission.AIArmyMissionErrorCode errorCode, out TickableState tickableState)
	{
		if (errorCode == AIArmyMission.AIArmyMissionErrorCode.InvalidDestination && this.PositionIndex + 1 < this.ListOfPosition.Length)
		{
			this.PositionIndex++;
			tickableState = TickableState.NoTick;
			return AICommanderMission.AICommanderMissionCompletion.Running;
		}
		return base.GetCompletionFor(errorCode, out tickableState);
	}

	protected override AICommanderMission.AICommanderMissionCompletion GetCompletionWhenSuccess(AIData_Army aiArmyData, out TickableState tickableState)
	{
		tickableState = TickableState.Optional;
		return AICommanderMission.AICommanderMissionCompletion.Success;
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
		return this.RegionTarget == null || this.RegionTarget.IsRegionColonized();
	}

	protected override bool MissionCanAcceptHero()
	{
		return false;
	}

	protected override void Pending()
	{
		base.Pending();
		this.State = TickableState.NoTick;
		Diagnostics.LogWarning("AICommanderMission in Pending fails to set MissionParameter to ArmyMission");
	}

	protected override void Running()
	{
		if (this.IsMissionCompleted() || this.IsRegionColonized())
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
	}

	protected override void Success()
	{
		base.Success();
	}

	protected override bool TryComputeArmyMissionParameter()
	{
		if (this.RegionTarget == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		base.ArmyMissionParameters.Clear();
		return !(base.AIDataArmyGUID == GameEntityGUID.Zero) && (this.PositionIndex < this.ListOfPosition.Length && base.TryCreateArmyMission("ColonizeAt", new List<object>
		{
			this.ListOfPosition[this.PositionIndex]
		}));
	}
}
