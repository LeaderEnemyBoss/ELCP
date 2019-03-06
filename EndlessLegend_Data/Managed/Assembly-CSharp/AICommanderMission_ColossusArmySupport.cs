using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Session;

public class AICommanderMission_ColossusArmySupport : AICommanderMission
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		this.departmentOfDefense = base.Commander.Empire.GetAgency<DepartmentOfDefense>();
		this.aiDataRepositoryHelper = AIScheduler.Services.GetService<IAIDataRepositoryAIHelper>();
		this.colossusCommander = (commander as AICommander_Colossus);
		base.AIDataArmyGUID = commander.ForceArmyGUID;
		if (base.Commander.Empire != null && base.Commander.Empire is MajorEmpire)
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
	}

	public override void Release()
	{
		base.Release();
		this.ailayer_War = null;
	}

	protected override void Running()
	{
		if (!this.aiDataRepository.IsGUIDValid(base.AIDataArmyGUID))
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
		if (aidata == null)
		{
			return false;
		}
		if (this.colossusCommander.CurrentColossusMission == null)
		{
			return false;
		}
		Diagnostics.Assert(aidata != null);
		Diagnostics.Assert(aidata.Army != null);
		if (base.Commander.Empire != null && base.Commander.Empire is MajorEmpire && this.ailayer_War != null)
		{
			this.ailayer_War.AssignDefensiveArmyToCity(aidata.Army);
		}
		Army item = this.ChooseTheBetterArmyToSupport();
		return base.TryCreateArmyMission("RoamingArmySupport", new List<object>
		{
			item
		});
	}

	private Army ChooseTheBetterArmyToSupport()
	{
		AIData_Army aidata;
		if (this.aiDataRepository.TryGetAIData<AIData_Army>(this.colossusCommander.CurrentColossusMission.TargetGuid, out aidata))
		{
			return aidata.Army;
		}
		float num = 0f;
		Army result = null;
		for (int i = 0; i < this.departmentOfDefense.Armies.Count; i++)
		{
			aidata = this.aiDataRepositoryHelper.GetAIData<AIData_Army>(this.departmentOfDefense.Armies[i].GUID);
			if (aidata != null && num < aidata.SupportScore)
			{
				num = aidata.SupportScore;
				result = this.departmentOfDefense.Armies[i];
			}
		}
		return result;
	}

	private IAIDataRepositoryAIHelper aiDataRepositoryHelper;

	private AICommander_Colossus colossusCommander;

	private DepartmentOfDefense departmentOfDefense;

	private AILayer_War ailayer_War;
}
