using System;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Game.Orders;

public class CatspawBehavior_DenySettlerAttack : CatspawBehavior
{
	public override string BehaviorName
	{
		get
		{
			return "Deny settler attack";
		}
	}

	protected override BehaviorNode<CatspawArmy> InitializeRoot()
	{
		Condition<CatspawArmy> condition = new Condition<CatspawArmy>(new Func<CatspawArmy, bool>(base.IsCloseEnoughToAttackSecondary));
		Condition<CatspawArmy> condition2 = new Condition<CatspawArmy>(new Func<CatspawArmy, bool>(base.HasEnoughActionPoint));
		Condition<CatspawArmy> condition3 = new Condition<CatspawArmy>(new Func<CatspawArmy, bool>(base.HasMovementLeft));
		Condition<CatspawArmy> condition4 = new Condition<CatspawArmy>(new Func<CatspawArmy, bool>(base.IsSecondaryValid));
		Condition<CatspawArmy> condition5 = new Condition<CatspawArmy>(new Func<CatspawArmy, bool>(base.IsRoamingValid));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy>(new Func<CatspawArmy, BehaviorNodeReturnCode>(base.WaitForNextTurn));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy> action2 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy>(new Func<CatspawArmy, BehaviorNodeReturnCode>(base.ComputePathToSecondary));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy> action3 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy>(new Func<CatspawArmy, BehaviorNodeReturnCode>(base.ComputePathToRoaming));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy> action4 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy>(new Func<CatspawArmy, BehaviorNodeReturnCode>(base.ChooseSecondaryAttackableTarget));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy> action5 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<CatspawArmy>(new Func<CatspawArmy, BehaviorNodeReturnCode>(base.ChoosePositionAwayFromMain));
		OrderAction<CatspawArmy> orderAction = new OrderAction<CatspawArmy>(new Func<CatspawArmy, Amplitude.Unity.Game.Orders.Order>(base.AttackSecondary));
		OrderAction<CatspawArmy> orderAction2 = new OrderAction<CatspawArmy>(new Func<CatspawArmy, Amplitude.Unity.Game.Orders.Order>(base.MoveSecondary));
		OrderAction<CatspawArmy> orderAction3 = new OrderAction<CatspawArmy>(new Func<CatspawArmy, Amplitude.Unity.Game.Orders.Order>(base.MoveRoaming));
		Selector<CatspawArmy> selector = new Selector<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition4,
			action4
		});
		Sequence<CatspawArmy> sequence = new Sequence<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition2,
			orderAction
		});
		Selector<CatspawArmy> selector2 = new Selector<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			sequence,
			action
		});
		Sequence<CatspawArmy> sequence2 = new Sequence<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition,
			selector2
		});
		Sequence<CatspawArmy> sequence3 = new Sequence<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition3,
			action2,
			orderAction2
		});
		Selector<CatspawArmy> selector3 = new Selector<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			sequence2,
			sequence3
		});
		Sequence<CatspawArmy> sequence4 = new Sequence<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			selector,
			selector3
		});
		Selector<CatspawArmy> selector4 = new Selector<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition5,
			action5
		});
		Sequence<CatspawArmy> sequence5 = new Sequence<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			condition3,
			selector4,
			action3,
			orderAction3
		});
		return new Selector<CatspawArmy>(new BehaviorNode<CatspawArmy>[]
		{
			sequence4,
			sequence5,
			condition3,
			action
		});
	}
}
