using System;

namespace Amplitude.Unity.AI.BehaviourTree
{
	public class BehaviourTreeNode_Selector : BehaviourTreeNodeController
	{
		public BehaviourTreeNode_Selector()
		{
			this.Index = 0;
		}

		public BehaviourTreeNode Current
		{
			get
			{
				if (base.Children == null)
				{
					return null;
				}
				if (this.Index < 0 || this.Index >= base.Children.Length)
				{
					return null;
				}
				return base.Children[this.Index];
			}
		}

		public int Index { get; private set; }

		public override void Reset()
		{
			this.Index = 0;
			for (int i = 0; i < base.Children.Length; i++)
			{
				base.Children[i].Reset();
			}
		}

		public override State Execute(BehaviourTree behaviourTree, params object[] parameters)
		{
			State state = State.Success;
			if (this.Current != null)
			{
				do
				{
					state = this.Current.Execute(behaviourTree, parameters);
					if (state == State.Failure)
					{
						this.Index++;
						parameters = null;
					}
				}
				while (this.Current != null && state == State.Failure);
			}
			if (base.ShouldResetOnExecutionResult(state))
			{
				parameters = null;
				this.Reset();
				this.Execute(behaviourTree, parameters);
			}
			return state;
		}
	}
}
