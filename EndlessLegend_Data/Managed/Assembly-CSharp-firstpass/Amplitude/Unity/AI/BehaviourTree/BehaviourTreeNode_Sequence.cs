using System;
using System.Xml.Serialization;
using Amplitude.Unity.Framework;

namespace Amplitude.Unity.AI.BehaviourTree
{
	public class BehaviourTreeNode_Sequence : BehaviourTreeNodeController
	{
		public BehaviourTreeNode_Sequence()
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

		[XmlAttribute("Index")]
		public int Index { get; protected set; }

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
					if (state == State.Success)
					{
						int index = this.Index;
						this.Index = index + 1;
						parameters = null;
					}
				}
				while (this.Current != null && state == State.Success);
			}
			if (base.ShouldResetOnExecutionResult(state))
			{
				parameters = null;
				this.Reset();
				return this.Execute(behaviourTree, parameters);
			}
			if (Application.Preferences.EnableModdingTools && state == State.Failure)
			{
				if (this.Current == null)
				{
					behaviourTree.LastNodeName = base.Children[base.Children.Length - 1].GetType().ToString();
				}
				else
				{
					behaviourTree.LastNodeName = this.Current.GetType().ToString();
				}
				behaviourTree.LastDebugString = base.Debug;
			}
			return state;
		}
	}
}
