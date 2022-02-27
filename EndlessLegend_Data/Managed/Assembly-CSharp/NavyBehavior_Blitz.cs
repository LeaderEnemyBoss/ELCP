using System;
using Amplitude.Unity.AI.SimpleBehaviorTree;
using Amplitude.Unity.Game.Orders;

public class NavyBehavior_Blitz : NavyBehavior
{
	public override string BehaviorName
	{
		get
		{
			return "NavyBehavior_Blitz";
		}
	}

	protected override BehaviorNode<BaseNavyArmy> InitializeRoot()
	{
		Condition<BaseNavyArmy> condition = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.IsCloseEnoughToAttackMain));
		Condition<BaseNavyArmy> condition2 = new Condition<BaseNavyArmy>(new Func<BaseNavyArmy, bool>(base.IsMainTargetUnderBombardment));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.WaitForNextTick));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action2 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.WaitForNextTurn));
		Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy> action3 = new Amplitude.Unity.AI.SimpleBehaviorTree.Action<BaseNavyArmy>(new Func<BaseNavyArmy, BehaviorNodeReturnCode>(base.Optional));
		OrderAction<BaseNavyArmy> orderAction = new OrderAction<BaseNavyArmy>(new Func<BaseNavyArmy, Amplitude.Unity.Game.Orders.Order>(base.Bombard));
		Sequence<BaseNavyArmy> sequence = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			action3,
			action
		});
		Selector<BaseNavyArmy> selector = new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition2,
			orderAction
		});
		Sequence<BaseNavyArmy> sequence2 = new Sequence<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			condition,
			selector,
			sequence
		});
		return new Selector<BaseNavyArmy>(new BehaviorNode<BaseNavyArmy>[]
		{
			base.SearchRuinSequence(),
			sequence2,
			base.MoveMainWithOpportunity(),
			action2
		});
	}
}
