using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public class AICommanderMission_HuntRuin : AICommanderMission
{
	public override void Initialize(AICommander commander)
	{
		base.Initialize(commander);
		Services.GetService<IGameService>();
		base.AIDataArmyGUID = commander.ForceArmyGUID;
		this.ruinCommander = (base.Commander as AICommander_RuinHunter);
		IGameService service = Services.GetService<IGameService>();
		this.game = (service.Game as global::Game);
		if (this.ruinCommander == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
		}
	}

	public override void Release()
	{
		base.Release();
		this.ruinCommander = null;
		this.game = null;
	}

	protected override void Running()
	{
		if (!base.AIDataArmyGUID.IsValid || this.ruinCommander == null)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		if (base.Commander.IsMissionFinished(false))
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Success;
			return;
		}
		base.Running();
		if (this.ruinCommander.PointOfInterests.Count > 0)
		{
			AIData_Army aidata = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
			if (aidata.ArmyMission != null && aidata.ArmyMission.Completion == AIArmyMission.AIArmyMissionCompletion.Fail && aidata.ArmyMission.ErrorCode == AIArmyMission.AIArmyMissionErrorCode.PathNotFound)
			{
				if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
				{
					Diagnostics.Log("ELCP {0} {1} AICommanderMission_HuntRuin errorcode {2} target {3} {4} {5} {6}", new object[]
					{
						base.Commander.Empire,
						aidata.Army.LocalizedName,
						aidata.ArmyMission.ErrorCode,
						this.ruinCommander.PointOfInterests[0].WorldPosition,
						this.ruinCommander.PointOfInterests.Count,
						this.lastErrorTurn,
						this.errorTicks
					});
				}
				if (this.lastErrorTurn != this.game.Turn)
				{
					this.lastErrorTurn = this.game.Turn;
					this.errorTicks = 1;
				}
				else
				{
					this.errorTicks++;
					if (this.errorTicks > 2)
					{
						this.ruinCommander.PointOfInterests.RemoveAt(0);
						this.errorTicks = 0;
					}
				}
			}
		}
		if (base.State == TickableState.NoTick && this.ruinCommander.PointOfInterests.Count > 0)
		{
			AIData_Army aidata2 = this.aiDataRepository.GetAIData<AIData_Army>(base.AIDataArmyGUID);
			if (aidata2 != null && aidata2.Army != null && aidata2.Army.GetPropertyValue(SimulationProperties.Movement) > 0.01f)
			{
				base.State = TickableState.Optional;
			}
		}
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
		if (this.ruinCommander.PointOfInterests.Count == 0)
		{
			base.Completion = AICommanderMission.AICommanderMissionCompletion.Fail;
			return false;
		}
		this.ruinCommander.RegionIndex = this.ruinCommander.PointOfInterests[0].Region.Index;
		return base.TryCreateArmyMission("HuntRuin", new List<object>
		{
			this.ruinCommander.PointOfInterests[0]
		});
	}

	private AICommander_RuinHunter ruinCommander;

	private int errorTicks;

	private int lastErrorTurn;

	private global::Game game;
}
