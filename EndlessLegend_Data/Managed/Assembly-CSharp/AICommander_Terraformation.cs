using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;
using Amplitude.Xml.Serialization;

public class AICommander_Terraformation : AICommanderWithObjective, IXmlSerializable
{
	public AICommander_Terraformation(ulong globalObjectiveID, int regionIndex) : base(AICommanderMissionDefinition.AICommanderCategory.Terraformation, globalObjectiveID, regionIndex)
	{
		this.RegionTarget = null;
		this.terraformPosition = WorldPosition.Invalid;
		this.deviceDefinitionName = string.Empty;
	}

	public AICommander_Terraformation() : base(AICommanderMissionDefinition.AICommanderCategory.Terraformation, 0UL, 0)
	{
	}

	public override void ReadXml(XmlReader reader)
	{
		int attribute = reader.GetAttribute<int>("RegionTargetIndex");
		this.RegionTarget = null;
		if (attribute != -1)
		{
			IGameService service = Services.GetService<IGameService>();
			Diagnostics.Assert(service != null);
			global::Game game = service.Game as global::Game;
			World world = game.World;
			this.RegionTarget = world.Regions[attribute];
			Diagnostics.Assert(this.RegionTarget != null);
		}
		this.terraformMessageId = reader.GetAttribute<ulong>("TerraformMessageId");
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
		writer.WriteAttributeString<ulong>("TerraformMessageId", this.terraformMessageId);
		writer.WriteAttributeString<short>("Row", this.TerraformPosition.Row);
		writer.WriteAttributeString<short>("Column", this.TerraformPosition.Column);
		writer.WriteAttributeString<string>("DeviceDefinitionName", this.deviceDefinitionName);
		base.WriteXml(writer);
	}

	public WorldPosition TerraformPosition
	{
		get
		{
			return this.terraformPosition;
		}
	}

	public Region RegionTarget
	{
		get
		{
			return this.region;
		}
		set
		{
			this.region = value;
			if (value != null)
			{
				base.RegionIndex = value.Index;
			}
		}
	}

	public string DeviceDefinitionName
	{
		get
		{
			return this.deviceDefinitionName;
		}
	}

	public override void Load()
	{
		base.Load();
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		Diagnostics.Assert(this.gameEntityRepositoryService != null);
		this.worldPositioningService = service.Game.Services.GetService<IWorldPositionningService>();
		Diagnostics.Assert(this.worldPositioningService != null);
		this.terraformDeviceService = service.Game.Services.GetService<ITerraformDeviceService>();
		Diagnostics.Assert(this.terraformDeviceService != null);
		this.terraformDeviceDatabase = Databases.GetDatabase<TerraformDeviceDefinition>(false);
		Diagnostics.Assert(this.terraformDeviceDatabase != null);
	}

	public override void PopulateMission()
	{
		Tags tags = new Tags();
		tags.AddTag(base.Category.ToString());
		if (string.IsNullOrEmpty(this.deviceDefinitionName) && !this.SelectTerraformDeviceDefinition(out this.deviceDefinitionName))
		{
			GlobalObjectiveMessage globalObjectiveMessage;
			if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage))
			{
				globalObjectiveMessage.State = BlackboardMessage.StateValue.Message_Canceled;
			}
			return;
		}
		if ((this.terraformPosition == WorldPosition.Invalid || !this.terraformDeviceService.IsPositionValidForDevice(base.Empire, this.terraformPosition)) && !this.SelectPositionToTerraform(this.RegionTarget, out this.terraformPosition))
		{
			GlobalObjectiveMessage globalObjectiveMessage2;
			if (base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage2))
			{
				globalObjectiveMessage2.State = BlackboardMessage.StateValue.Message_Canceled;
			}
			return;
		}
		this.SendTerraformAction();
		for (int i = base.Missions.Count - 1; i >= 0; i--)
		{
			if (base.Missions[i].MissionDefinition.Category.Contains(tags))
			{
				return;
			}
			this.CancelMission(base.Missions[i]);
		}
		base.PopulationFirstMissionFromCategory(tags, new object[]
		{
			this.RegionTarget,
			this.TerraformPosition.Column,
			this.TerraformPosition.Row,
			this.DeviceDefinitionName
		});
	}

	private void SendTerraformAction()
	{
		EvaluableMessage_Terraform evaluableMessage_Terraform = base.AIPlayer.Blackboard.FindFirst<EvaluableMessage_Terraform>(BlackboardLayerID.Empire, (EvaluableMessage_Terraform match) => match.RegionIndex == base.RegionIndex && match.TerraformPosition == this.terraformPosition && match.DeviceDefinitionName == this.DeviceDefinitionName);
		if (evaluableMessage_Terraform == null || evaluableMessage_Terraform.EvaluationState == EvaluableMessage.EvaluableMessageState.Cancel)
		{
			evaluableMessage_Terraform = new EvaluableMessage_Terraform(base.RegionIndex, this.TerraformPosition, this.DeviceDefinitionName);
			this.terraformMessageId = base.AIPlayer.Blackboard.AddMessage(evaluableMessage_Terraform);
		}
		else
		{
			this.terraformMessageId = evaluableMessage_Terraform.ID;
		}
		evaluableMessage_Terraform.TimeOut = 1;
		evaluableMessage_Terraform.SetInterest(base.GlobalPriority, base.LocalPriority);
	}

	private bool SelectPositionToTerraform(Region region, out WorldPosition position)
	{
		City city = region.City;
		if (city != null)
		{
			if (this.IsPositionValidToTerraform(city.WorldPosition))
			{
				position = city.WorldPosition;
				return true;
			}
			foreach (District district in city.Districts)
			{
				if (district != null && this.IsPositionValidToTerraform(district.WorldPosition))
				{
					position = district.WorldPosition;
					return true;
				}
			}
		}
		List<WorldPosition> list;
		this.GetNonVolcanicPositions(region, out list);
		if (list.Count > 0)
		{
			int index = this.random.Next(0, list.Count - 1);
			position = list[index];
			return true;
		}
		position = WorldPosition.Invalid;
		return false;
	}

	private void GetNonVolcanicPositions(Region region, out List<WorldPosition> positions)
	{
		positions = new List<WorldPosition>();
		WorldPosition[] worldPositions = region.WorldPositions;
		for (int i = 0; i < worldPositions.Length; i++)
		{
			if (this.IsPositionValidToTerraform(worldPositions[i]))
			{
				positions.Add(worldPositions[i]);
			}
		}
	}

	private bool IsPositionValidToTerraform(WorldPosition position)
	{
		return !this.worldPositioningService.ContainsTerrainTag(position, "TerrainTagVolcanic") && this.terraformDeviceService.IsPositionValidForDevice(base.Empire, position) && !this.terraformDeviceService.IsPositionNextToDevice(position);
	}

	private bool SelectTerraformDeviceDefinition(out string deviceDefinitionName)
	{
		TerraformDeviceDefinition[] values = this.terraformDeviceDatabase.GetValues();
		if (values.Length > 0)
		{
			int num = 0;
			if (values.Length > 1)
			{
				num = this.random.Next(0, values.Length - 1);
			}
			deviceDefinitionName = values[num].Name;
			return true;
		}
		deviceDefinitionName = string.Empty;
		return false;
	}

	public override bool IsMissionFinished(bool forceStep)
	{
		if (this.terraformMessageId != 0UL)
		{
			EvaluableMessage_Terraform evaluableMessage_Terraform = base.AIPlayer.Blackboard.GetMessage(this.terraformMessageId) as EvaluableMessage_Terraform;
			if (evaluableMessage_Terraform != null && (evaluableMessage_Terraform.State == BlackboardMessage.StateValue.Message_Canceled || evaluableMessage_Terraform.State == BlackboardMessage.StateValue.Message_Failed || evaluableMessage_Terraform.State == BlackboardMessage.StateValue.Message_Success))
			{
				return true;
			}
		}
		GlobalObjectiveMessage globalObjectiveMessage;
		return base.GlobalObjectiveID == 0UL || base.AIPlayer == null || !base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage) || (globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Canceled || globalObjectiveMessage.State == BlackboardMessage.StateValue.Message_Failed);
	}

	public override void Release()
	{
		GlobalObjectiveMessage globalObjectiveMessage;
		if (this.IsMissionFinished(false) && base.AIPlayer.Blackboard.TryGetMessage<GlobalObjectiveMessage>(base.GlobalObjectiveID, out globalObjectiveMessage))
		{
			globalObjectiveMessage.State = BlackboardMessage.StateValue.Message_Canceled;
		}
		if (this.terraformMessageId != 0UL)
		{
			EvaluableMessage_Terraform evaluableMessage_Terraform = base.AIPlayer.Blackboard.GetMessage(this.terraformMessageId) as EvaluableMessage_Terraform;
			if (evaluableMessage_Terraform != null)
			{
				if (evaluableMessage_Terraform.EvaluationState == EvaluableMessage.EvaluableMessageState.Validate || evaluableMessage_Terraform.EvaluationState == EvaluableMessage.EvaluableMessageState.Obtaining || evaluableMessage_Terraform.EvaluationState == EvaluableMessage.EvaluableMessageState.Pending)
				{
					evaluableMessage_Terraform.Cancel();
				}
				base.AIPlayer.Blackboard.CancelMessage(this.terraformMessageId);
			}
		}
		this.RegionTarget = null;
		this.terraformPosition = WorldPosition.Invalid;
		this.deviceDefinitionName = string.Empty;
		base.Release();
	}

	public override void RefreshMission()
	{
		base.RefreshMission();
		if (!this.IsMissionFinished(false))
		{
			this.PopulateMission();
			base.PromoteMission();
		}
	}

	private Random random = new Random();

	private IDatabase<TerraformDeviceDefinition> terraformDeviceDatabase;

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private IWorldPositionningService worldPositioningService;

	private ITerraformDeviceService terraformDeviceService;

	private Region region;

	private WorldPosition terraformPosition;

	private string deviceDefinitionName;

	private ulong terraformMessageId;
}
