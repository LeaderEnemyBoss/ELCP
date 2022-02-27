using System;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;

public abstract class CatspawTask : ArmyTask
{
	public CatspawTask(CatspawBehavior behavior)
	{
		base.Behavior = behavior;
		base.Behavior.Initialize();
		IGameService service = Services.GetService<IGameService>();
		this.worldPositionningService = service.Game.Services.GetService<IWorldPositionningService>();
	}

	public virtual bool IsMinorArmyValid(Army minorArmy, float armyLifeTime)
	{
		return base.AssignedArmy == null;
	}

	public virtual float GetLocalPriority()
	{
		return 0.5f;
	}

	public abstract WorldPosition GetTargetPosition();

	protected IWorldPositionningService worldPositionningService;
}
