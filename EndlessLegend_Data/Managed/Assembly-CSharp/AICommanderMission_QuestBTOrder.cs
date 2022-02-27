using System;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;

public class AICommanderMission_QuestBTOrder : AICommanderMissionWithRequestArmy
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("TargetGUID", this.TargetGUID);
		base.WriteXml(writer);
	}

	public override void ReadXml(XmlReader reader)
	{
		this.TargetGUID = reader.GetAttribute<ulong>("TargetGUID");
		base.ReadXml(reader);
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.TargetGUID = (GameEntityGUID)parameters[0];
	}

	public override void Load()
	{
		base.Load();
		if (!this.TargetGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		IGameEntity gameEntity;
		if (this.gameEntityRepositoryService.TryGetValue(this.TargetGUID, out gameEntity) && gameEntity is PointOfInterest)
		{
			this.POI = (gameEntity as PointOfInterest);
		}
		AIEntity_Empire entity = base.Commander.AIPlayer.GetEntity<AIEntity_Empire>();
		this.QuestBTlayer = entity.GetLayer<AILayer_QuestBTController>();
		if (this.POI != null)
		{
			this.village = this.POI.Region.MinorEmpire.GetAgency<BarbarianCouncil>().GetVillageAt(this.POI.WorldPosition);
			return;
		}
		if (gameEntity is TerraformDevice)
		{
			this.device = (gameEntity as TerraformDevice);
		}
	}

	public override void Release()
	{
		base.Release();
		this.POI = null;
		this.gameEntityRepositoryService = null;
		this.QuestBTlayer = null;
		this.village = null;
		this.device = null;
	}

	protected override void Running()
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		if (this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID).Army == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		if (base.Commander.IsMissionFinished(false))
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
		base.ArmyMissionParameters.Clear();
		if (base.AIDataArmyGUID == GameEntityGUID.Zero)
		{
			return false;
		}
		if (this.POI == null)
		{
			if (this.device != null)
			{
				return base.TryCreateArmyMission("DismantleQuestDevice", new List<object>
				{
					this.device
				});
			}
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		else
		{
			if (this.POI.Type != "Village")
			{
				return base.TryCreateArmyMission("VisitQuestRuin", new List<object>
				{
					(base.Commander as AICommanderWithObjective).RegionIndex,
					this.POI
				});
			}
			return base.TryCreateArmyMission("VisitQuestVillage", new List<object>
			{
				this.village
			});
		}
	}

	private PointOfInterest POI { get; set; }

	private GameEntityGUID TargetGUID { get; set; }

	protected override void GetNeededArmyPower(out float minMilitaryPower, out bool isMaxPower, out bool perUnitTest)
	{
		isMaxPower = false;
		perUnitTest = false;
		minMilitaryPower = 1f;
		AILayer_QuestBTController.QuestBTOrder questBTOrder;
		if (this.QuestBTlayer.TryGetQuestBTOrder((base.Commander as AICommander_QuestBTCommander).QuestName, this.TargetGUID, out questBTOrder))
		{
			minMilitaryPower = questBTOrder.requiredArmyPower;
		}
	}

	protected override int GetNeededAvailabilityTime()
	{
		return 5;
	}

	private IGameEntityRepositoryService gameEntityRepositoryService;

	private AILayer_QuestBTController QuestBTlayer;

	private Village village;

	private TerraformDevice device;
}
