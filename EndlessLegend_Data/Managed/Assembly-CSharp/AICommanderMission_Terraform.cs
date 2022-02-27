using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommanderMission_Terraform : AICommanderMissionWithRequestArmy, IXmlSerializable
{
	public AICommanderMission_Terraform()
	{
		this.RegionTarget = null;
		this.terraformPosition = WorldPosition.Invalid;
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
		WorldPosition worldPosition;
		worldPosition.Row = reader.GetAttribute<short>("Row");
		worldPosition.Column = reader.GetAttribute<short>("Column");
		this.terraformPosition = worldPosition;
		this.deviceDefinitionName = reader.GetAttribute<string>("DeviceDefinitionName");
		base.ReadXml(reader);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<int>("RegionTargetIndex", (this.RegionTarget != null) ? this.RegionTarget.Index : -1);
		writer.WriteAttributeString<short>("Row", this.TerraformPosition.Row);
		writer.WriteAttributeString<short>("Column", this.TerraformPosition.Column);
		writer.WriteAttributeString<string>("DeviceDefinitionName", this.DeviceDefinitionName);
		base.WriteXml(writer);
	}

	public Region RegionTarget { get; set; }

	public WorldPosition TerraformPosition
	{
		get
		{
			return this.terraformPosition;
		}
	}

	public string DeviceDefinitionName
	{
		get
		{
			return this.deviceDefinitionName;
		}
	}

	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
	}

	public override void Load()
	{
		base.Load();
		if (this.RegionTarget == null || this.TerraformPosition == WorldPosition.Invalid || this.DeviceDefinitionName == string.Empty)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
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

	public override void Release()
	{
		base.Release();
		this.RegionTarget = null;
		this.terraformPosition = WorldPosition.Invalid;
		this.deviceDefinitionName = string.Empty;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.RegionTarget = (parameters[0] as Region);
		this.terraformPosition.Column = (short)parameters[1];
		this.terraformPosition.Row = (short)parameters[2];
		this.deviceDefinitionName = (string)parameters[3];
	}

	public override WorldPosition GetTargetPositionForTheArmy()
	{
		return this.TerraformPosition;
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
		if (this.TerraformPosition == WorldPosition.Invalid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		return base.TryCreateArmyMission("Terraform", new List<object>
		{
			this.RegionTarget.Index,
			this.TerraformPosition,
			this.DeviceDefinitionName
		});
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

	protected override bool IsMissionCompleted()
	{
		AICommander_Terraformation commanderObjective = base.Commander as AICommander_Terraformation;
		EvaluableMessage_Terraform evaluableMessage_Terraform = commanderObjective.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Terraform>(BlackboardLayerID.Empire, (EvaluableMessage_Terraform match) => match.RegionIndex == commanderObjective.RegionIndex && match.TerraformPosition == commanderObjective.TerraformPosition && match.DeviceDefinitionName == this.DeviceDefinitionName);
		return evaluableMessage_Terraform == null || evaluableMessage_Terraform.State != BlackboardMessage.StateValue.Message_InProgress;
	}

	private WorldPosition terraformPosition;

	private string deviceDefinitionName;
}
