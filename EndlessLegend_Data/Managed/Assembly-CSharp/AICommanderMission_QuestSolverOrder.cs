using System;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Xml;

public class AICommanderMission_QuestSolverOrder : AICommanderMission
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		IGameService service = Services.GetService<IGameService>();
		this.gameEntityRepositoryService = service.Game.Services.GetService<IGameEntityRepositoryService>();
		base.AIDataArmyGUID = commander.ForceArmyGUID;
	}

	public override void SetParameters(AICommanderMissionDefinition missionDefinition, params object[] parameters)
	{
		base.SetParameters(missionDefinition, parameters);
		this.targetGUID = (GameEntityGUID)parameters[0];
	}

	public override void Load()
	{
		base.Load();
		if (!this.targetGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return;
		}
		IGameEntity gameEntity;
		this.gameEntityRepositoryService.TryGetValue(this.targetGUID, out gameEntity);
		if (gameEntity is City)
		{
			this.city = (gameEntity as City);
			return;
		}
		base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
	}

	public override void Release()
	{
		base.Release();
		this.city = null;
		this.gameEntityRepositoryService = null;
	}

	protected override void Running()
	{
		if (!base.AIDataArmyGUID.IsValid)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		if (base.Commander.IsMissionFinished(false))
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (aidata == null || aidata.Army == null)
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
		AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
		if (this.city == null || aidata == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		List<object> list = new List<object>();
		list.Add(this.city);
		if (this.city.BesiegingEmpireIndex >= 0)
		{
			return base.TryCreateArmyMission("FreeCity", list);
		}
		if (this.city.MaximumUnitSlot > this.city.CurrentUnitSlot + aidata.Army.CurrentUnitSlot)
		{
			return base.TryCreateArmyMission("DefendCity_Bail", list);
		}
		return base.TryCreateArmyMission("ReachTarget", list);
	}

	public override void WriteXml(XmlWriter writer)
	{
		writer.WriteAttributeString<ulong>("TargetGUID", this.targetGUID);
		base.WriteXml(writer);
	}

	public override void ReadXml(XmlReader reader)
	{
		this.targetGUID = reader.GetAttribute<ulong>("TargetGUID");
		base.ReadXml(reader);
	}

	private City city;

	private GameEntityGUID targetGUID;

	private IGameEntityRepositoryService gameEntityRepositoryService;
}
