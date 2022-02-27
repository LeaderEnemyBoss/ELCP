using System;

public class CatspawTask_DenySettlerAttack : CatspawTask
{
	public CatspawTask_DenySettlerAttack() : base(new CatspawBehavior_DenySettlerAttack())
	{
	}

	public Army SettlerArmy
	{
		get
		{
			return this.settlerArmy;
		}
		set
		{
			this.settlerArmy = value;
			if (this.settlerArmy != null)
			{
				base.TargetGuid = this.settlerArmy.GUID;
			}
			else
			{
				base.TargetGuid = GameEntityGUID.Zero;
			}
		}
	}

	public override bool CheckValidity()
	{
		if (this.settlerArmy.SimulationObject == null || this.settlerArmy.StandardUnits.Count == 0)
		{
			return false;
		}
		if (base.AssignedArmy != null)
		{
			int regionIndex = (int)this.worldPositionningService.GetRegionIndex(this.settlerArmy.WorldPosition);
			int regionIndex2 = (int)this.worldPositionningService.GetRegionIndex(base.AssignedArmy.Garrison.WorldPosition);
			if (regionIndex != regionIndex2)
			{
				return false;
			}
		}
		return true;
	}

	public override bool IsMinorArmyValid(Army minorArmy, float armyLifeTime)
	{
		if (!base.IsMinorArmyValid(minorArmy, armyLifeTime))
		{
			return false;
		}
		if (minorArmy.HasSeafaringUnits() != this.SettlerArmy.IsNaval)
		{
			return false;
		}
		int regionIndex = (int)this.worldPositionningService.GetRegionIndex(this.settlerArmy.WorldPosition);
		int regionIndex2 = (int)this.worldPositionningService.GetRegionIndex(minorArmy.WorldPosition);
		return regionIndex == regionIndex2;
	}

	public override WorldPosition GetTargetPosition()
	{
		return this.SettlerArmy.WorldPosition;
	}

	private Army settlerArmy;
}
