using System;
using System.Xml.Serialization;

namespace Amplitude.Unity.AI.BehaviourTree
{
	public abstract class BehaviourTreeNodeController : BehaviourTreeNode
	{
		public BehaviourTreeNodeController()
		{
			this.CompletionPolicy = CompletionPolicy.All;
			this.ResetPolicy = BehaviourTreeNodeController.ResetPolicies.none;
		}

		[XmlElement(Type = typeof(BehaviourTreeNode_Selector), ElementName = "Controller_Selector")]
		[XmlElement(Type = typeof(BehaviourTreeNode_Sequence), ElementName = "Controller_Sequence")]
		[XmlElement(Type = typeof(BehaviourTreeNode_Loop), ElementName = "Controller_Loop")]
		[XmlElement(Type = typeof(BehaviourTreeNode_Parallel), ElementName = "Controller_Parallel")]
		public BehaviourTreeNode[] Children { get; set; }

		[XmlAttribute("CompletionPolicy")]
		public CompletionPolicy CompletionPolicy { get; set; }

		[XmlAttribute("ResetPolicy")]
		public BehaviourTreeNodeController.ResetPolicies ResetPolicy { get; set; }

		public override object Clone()
		{
			BehaviourTreeNodeController behaviourTreeNodeController = (BehaviourTreeNodeController)base.MemberwiseClone();
			if (this.Children != null)
			{
				behaviourTreeNodeController.Children = new BehaviourTreeNode[this.Children.Length];
				for (int i = 0; i < this.Children.Length; i++)
				{
					behaviourTreeNodeController.Children[i] = (BehaviourTreeNode)this.Children[i].Clone();
				}
			}
			else
			{
				Diagnostics.LogError("A node controller should define at least one children. Node.Type = '{0}'.", new object[]
				{
					base.GetType()
				});
			}
			return behaviourTreeNodeController;
		}

		public override State Execute(BehaviourTree behaviourTree, params object[] parameters)
		{
			return base.Execute(behaviourTree, parameters);
		}

		public override bool Initialize(BehaviourTree behaviourTree)
		{
			bool flag = true;
			if (this.Children == null)
			{
				Diagnostics.LogError("A node controller should define at least one children. Node.Type = '{0}'.", new object[]
				{
					base.GetType()
				});
				return false;
			}
			for (int i = 0; i < this.Children.Length; i++)
			{
				flag &= this.Children[i].Initialize(behaviourTree);
			}
			return flag;
		}

		public bool ShouldResetOnExecutionResult(State executionResult)
		{
			return (executionResult == State.Failure && this.ResetPolicy == BehaviourTreeNodeController.ResetPolicies.onFail) || (executionResult == State.Success && this.ResetPolicy == BehaviourTreeNodeController.ResetPolicies.onCompletion);
		}

		public enum ResetPolicies
		{
			none,
			onFail,
			onCompletion
		}
	}
}
