using System;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class CatspawArmy : ArmyWithTask
{
	public CatspawArmy(Army army, AILayer_Catspaw catspawLayer)
	{
		this.army = army;
		base.Garrison = this.army;
		this.catspawLayer = catspawLayer;
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
	}

	public IGarrison SecondaryAttackableTarget { get; set; }

	public WorldPath PathToSecondaryTarget { get; set; }

	public WorldPosition RoamingPosition { get; set; }

	public WorldPath PathToRoamingPosition { get; set; }

	public CatspawArmy.ReleasePolicyType ReleasePolicy { get; set; }

	public override bool ValidateMainTask()
	{
		if (base.CurrentMainTask != null)
		{
			if (base.CurrentMainTask.CheckValidity())
			{
				return false;
			}
			this.Unassign();
		}
		ArmyTask armyTask = null;
		float num = float.MaxValue;
		for (int i = 0; i < this.catspawLayer.CatspawTasks.Count; i++)
		{
			if (this.catspawLayer.CatspawTasks[i].CheckValidity() && this.catspawLayer.CatspawTasks[i].IsMinorArmyValid(this.army, 5f))
			{
				float num2 = (float)this.worldPositionningService.GetDistance(base.Garrison.WorldPosition, this.catspawLayer.CatspawTasks[i].GetTargetPosition());
				if (num > num2)
				{
					armyTask = this.catspawLayer.CatspawTasks[i];
					num = num2;
				}
			}
		}
		if (base.CurrentMainTask != armyTask)
		{
			this.Assign(armyTask, new HeuristicValue(0f));
			return true;
		}
		return false;
	}

	public override void Tick()
	{
		if (!this.IsActive)
		{
			base.State = TickableState.NoTick;
			return;
		}
		if (this.ReleasePolicy == CatspawArmy.ReleasePolicyType.Force)
		{
			this.ReleaseCatspaw();
			return;
		}
		base.Tick();
		if (this.ReleasePolicy == CatspawArmy.ReleasePolicyType.OnEndTurn)
		{
			if (base.State == TickableState.NoTick || base.State == TickableState.Optional)
			{
				this.ReleaseCatspaw();
				return;
			}
		}
		else if (this.ReleasePolicy == CatspawArmy.ReleasePolicyType.OnSucceed && base.BehaviorState == ArmyWithTask.ArmyBehaviorState.Succeed)
		{
			this.ReleaseCatspaw();
		}
	}

	protected override ArmyBehavior GetDefaultBehavior()
	{
		return new CatspawBehavior_Roaming();
	}

	private void ReleaseCatspaw()
	{
		OrderToggleCatspaw order = new OrderToggleCatspaw(base.Garrison.Empire.Index, base.Garrison.GUID, false);
		base.Garrison.Empire.PlayerControllers.AI.PostOrder(order);
		this.State = TickableState.NoTick;
	}

	public override void DisplayDebug()
	{
		GUILayout.Label(string.Format("ReleasePolicy : {0}", this.ReleasePolicy), new GUILayoutOption[0]);
		base.DisplayDebug();
	}

	private AILayer_Catspaw catspawLayer;

	private Army army;

	private IWorldPositionningService worldPositionningService;

	public enum ReleasePolicyType
	{
		None,
		Force,
		OnSucceed,
		OnEndTurn
	}
}
