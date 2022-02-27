using System;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Game.Orders;

public class NavyBehavior_Interception : NavyBehavior
{
	public override string BehaviorName
	{
		get
		{
			return "NavyInterception";
		}
	}

	protected override BehaviorNode<BaseNavyArmy> InitializeRoot()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.IsCloseEnoughToAttackMain));
		Condition<BaseNavyArmy> condition2 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.HasEnoughActionPoint));
		Condition<BaseNavyArmy> condition3 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.CanReachTargetThisTurn));
		Condition<BaseNavyArmy> condition4 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.HasMovementLeft));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.WaitForNextTick));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action2 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.InvalidateBehavior));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action3 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.WaitForNextTurn));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action4 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.Optional));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(base.Attack));
		OrderAction<BaseNavyArmy> orderAction2 = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(base.GotoAndAttackMain));
		Sequence<BaseNavyArmy> sequence = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			action4,
			action
		});
		Sequence<BaseNavyArmy> sequence2 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition2,
			orderAction
		});
		Selector<BaseNavyArmy> selector = new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			sequence2,
			sequence
		});
		Sequence<BaseNavyArmy> sequence3 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			selector
		});
		Sequence<BaseNavyArmy> sequence4 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition4,
			condition2,
			condition3,
			orderAction2
		});
		Sequence<BaseNavyArmy> sequence5 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition4,
			action2
		});
		return new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			base.SearchRuinSequence(),
			sequence3,
			sequence4,
			base.MoveMainWithOpportunity(),
			sequence5,
			action3
		});
	}
}
