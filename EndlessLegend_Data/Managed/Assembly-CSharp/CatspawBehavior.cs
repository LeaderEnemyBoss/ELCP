using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Game.Orders;
using UnityEngine;

public abstract class CatspawBehavior : ArmyBehavior
{
	public override void Initialize()
	{
		base.Initialize();
		this.visibilityService = this.gameService.Game.Services.GetService<IVisibilityService>();
	}

	protected override BehaviorNode<ArmyWithTask> GenerateRoot()
	{
		return new CatspawBehavior.ConverterNode(this.InitializeRoot());
	}

	protected abstract BehaviorNode<CatspawArmy> InitializeRoot();

	protected BehaviorNodeReturnCode ComputePathToSecondary(CatspawArmy army)
	{
		if (army.SecondaryAttackableTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		IGameEntityWithWorldPosition gameEntityWithWorldPosition = army.SecondaryAttackableTarget as IGameEntityWithWorldPosition;
		if (gameEntityWithWorldPosition == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		WorldPosition validTileToAttack = base.GetValidTileToAttack(army, gameEntityWithWorldPosition);
		if (!validTileToAttack.IsValid)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		army.PathToSecondaryTarget = base.ComputePathToPosition(army, validTileToAttack, army.PathToSecondaryTarget);
		if (army.PathToSecondaryTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode ComputePathToRoaming(CatspawArmy army)
	{
		if (!this.IsRoamingValid(army))
		{
			return BehaviorNodeReturnCode.Failure;
		}
		army.PathToRoamingPosition = base.ComputePathToPosition(army, army.RoamingPosition, army.PathToRoamingPosition);
		if (army.PathToRoamingPosition == null)
		{
			army.RoamingPosition = WorldPosition.Invalid;
			return BehaviorNodeReturnCode.Failure;
		}
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode ChooseSecondaryAttackableTarget(CatspawArmy army)
	{
		List<Army> list = new List<Army>();
		this.FillVisibleArmiesAround(army.Garrison, list);
		if (list.Count == 0)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		list.Sort(delegate(Army left, Army right)
		{
			int distance = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, left.WorldPosition);
			int distance2 = this.worldPositionService.GetDistance(army.Garrison.WorldPosition, right.WorldPosition);
			return distance.CompareTo(distance2);
		});
		army.SecondaryAttackableTarget = list[0];
		return BehaviorNodeReturnCode.Success;
	}

	protected BehaviorNodeReturnCode ChoosePositionAwayFromMain(CatspawArmy army)
	{
		IWorldPositionable mainAttackableTarget = army.MainAttackableTarget;
		if (mainAttackableTarget == null)
		{
			return BehaviorNodeReturnCode.Failure;
		}
		army.RoamingPosition = mainAttackableTarget.WorldPosition;
		if (this.worldPositionService.GetDistance(army.RoamingPosition, army.Garrison.WorldPosition) < 2)
		{
			WorldPosition validTileToAttack = base.GetValidTileToAttack(army, army.Garrison);
			if (validTileToAttack.IsValid)
			{
				army.RoamingPosition = validTileToAttack;
			}
		}
		return BehaviorNodeReturnCode.Success;
	}

	protected void FillVisibleArmiesAround(IGarrisonWithPosition garrison, List<Army> targetArmies)
	{
		DepartmentOfForeignAffairs agency = garrison.Empire.GetAgency<DepartmentOfForeignAffairs>();
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		global::Game game = service.Game as global::Game;
		int num = Mathf.RoundToInt(garrison.GetPropertyValue(SimulationProperties.VisionRange));
		for (int i = 0; i < game.Empires.Length; i++)
		{
			global::Empire empire = game.Empires[i];
			if (garrison.Empire != empire)
			{
				if (!(empire is MajorEmpire) || !agency.IsFriend(empire))
				{
					DepartmentOfDefense agency2 = empire.GetAgency<DepartmentOfDefense>();
					if (agency2 != null)
					{
						for (int j = 0; j < agency2.Armies.Count; j++)
						{
							Army army = agency2.Armies[j];
							if (army.UnitsCount != 0)
							{
								if (!army.IsCamouflaged || this.visibilityService.IsWorldPositionDetectedFor(army.WorldPosition, garrison.Empire))
								{
									if (this.worldPositionService.GetDistance(army.WorldPosition, garrison.WorldPosition) <= num)
									{
										targetArmies.Add(army);
									}
								}
							}
						}
					}
				}
			}
		}
	}

	protected bool IsSecondaryValid(CatspawArmy army)
	{
		return army.SecondaryAttackableTarget != null && army.SecondaryAttackableTarget.UnitsCount != 0;
	}

	protected bool IsRoamingValid(CatspawArmy army)
	{
		return army.RoamingPosition.IsValid;
	}

	protected bool IsCloseEnoughToAttackSecondary(CatspawArmy army)
	{
		if (army.SecondaryAttackableTarget == null)
		{
			return false;
		}
		Army army2 = army.Garrison as Army;
		return army2 != null && base.IsCloseEnoughToAttack(army2, army.SecondaryAttackableTarget);
	}

	protected Amplitude.Unity.Game.Orders.Order AttackSecondary(CatspawArmy army)
	{
		if (!this.IsCloseEnoughToAttackSecondary(army))
		{
			return null;
		}
		if (!base.HasEnoughActionPoint(army))
		{
			return null;
		}
		return new OrderAttack(army.Garrison.Empire.Index, army.Garrison.GUID, army.SecondaryAttackableTarget.GUID);
	}

	protected Amplitude.Unity.Game.Orders.Order MoveSecondary(CatspawArmy army)
	{
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		return base.FollowPath(army, army.PathToSecondaryTarget);
	}

	protected Amplitude.Unity.Game.Orders.Order MoveRoaming(CatspawArmy army)
	{
		if (!base.HasMovementLeft(army))
		{
			return null;
		}
		return base.FollowPath(army, army.PathToRoamingPosition);
	}

	protected IVisibilityService visibilityService;

	public class ConverterNode : BehaviorNode<ArmyWithTask>
	{
		public ConverterNode(BehaviorNode<CatspawArmy> catspawNode)
		{
			this.catspawNode = catspawNode;
		}

		public override BehaviorNodeReturnCode Behave(ArmyWithTask behaviorData)
		{
			return this.catspawNode.Behave(behaviorData as CatspawArmy);
		}

		public override BehaviorNodeDebug DumpDebug()
		{
			return this.catspawNode.DumpDebug();
		}

		public override void Reset()
		{
			this.catspawNode.Reset();
		}

		private BehaviorNode<CatspawArmy> catspawNode;
	}
}
