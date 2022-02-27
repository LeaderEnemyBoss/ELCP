using System;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.AI.BehaviourTree;
using UnityEngine;

public class QuestBehaviourTreeNode_Action_TransferResource : QuestBehaviourTreeNode_Action
{
	public QuestBehaviourTreeNode_Action_TransferResource()
	{
		this.ResourceName = string.Empty;
		this.Amount = -1;
	}

	[XmlAttribute("AmountVarName")]
	public string AmountVarName { get; set; }

	[XmlAttribute]
	public string EmpireIndexVarName { get; set; }

	[XmlAttribute("ResourceNameVarName")]
	public string ResourceNameVarName { get; set; }

	[XmlElement]
	public int Amount { get; set; }

	[XmlElement]
	public string ResourceName { get; set; }

	protected override State Execute(QuestBehaviour questBehaviour, params object[] parameters)
	{
		if (!string.IsNullOrEmpty(this.ResourceName))
		{
			if (!string.IsNullOrEmpty(this.EmpireIndexVarName))
			{
				int num = -1;
				if (!questBehaviour.TryGetQuestVariableValueByName<int>(this.EmpireIndexVarName, out num))
				{
					Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
					{
						this.EmpireIndexVarName
					});
					return State.Failure;
				}
				if (num != questBehaviour.Initiator.Index)
				{
					return State.Success;
				}
			}
			if (questBehaviour.Initiator is MajorEmpire && (questBehaviour.Initiator as MajorEmpire).ELCPIsEliminated)
			{
				return State.Success;
			}
			if (this.ResourceName.Contains("Booster"))
			{
				for (int i = 0; i < this.Amount; i++)
				{
					OrderBuyoutAndActivateBooster order = new OrderBuyoutAndActivateBooster(questBehaviour.Initiator.Index, this.ResourceName, 0UL, false);
					questBehaviour.Initiator.PlayerControllers.Server.PostOrder(order);
				}
			}
			else
			{
				Order order2 = new OrderTransferResources(questBehaviour.Initiator.Index, this.ResourceName, (float)this.Amount, 0UL);
				questBehaviour.Initiator.PlayerControllers.Server.PostOrder(order2);
			}
		}
		return State.Success;
	}

	protected override bool Initialize(QuestBehaviour questBehaviour)
	{
		if (string.IsNullOrEmpty(this.ResourceName))
		{
			string text;
			if (questBehaviour.TryGetQuestVariableValueByName<string>(this.ResourceNameVarName, out text))
			{
				if (string.IsNullOrEmpty(text))
				{
					Diagnostics.LogError("Resource name is null or empty, quest variable (varname: '{0}')", new object[]
					{
						this.ResourceNameVarName
					});
					return false;
				}
				this.ResourceName = text;
			}
			else
			{
				IDroppable droppable;
				if (!questBehaviour.TryGetQuestVariableValueByName<IDroppable>(this.ResourceNameVarName, out droppable))
				{
					Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
					{
						this.ResourceNameVarName
					});
					return false;
				}
				DroppableString droppableString = droppable as DroppableString;
				if (droppableString == null || string.IsNullOrEmpty(droppableString.Value))
				{
					Diagnostics.LogError("Resource name is null or empty, quest variable (varname: '{0}')", new object[]
					{
						this.ResourceNameVarName
					});
					return false;
				}
				this.ResourceName = droppableString.Value;
			}
		}
		if (this.Amount == -1)
		{
			QuestRegisterVariable questRegisterVariable;
			IDroppable droppable2;
			if (questBehaviour.TryGetQuestVariableValueByName<QuestRegisterVariable>(this.AmountVarName, out questRegisterVariable))
			{
				if (questRegisterVariable == null)
				{
					Diagnostics.LogError("QuestRegisterVariable is null, quest variable (varname: '{0}')", new object[]
					{
						this.ResourceNameVarName
					});
					return false;
				}
				this.Amount = questRegisterVariable.Value;
			}
			else if (questBehaviour.TryGetQuestVariableValueByName<IDroppable>(this.AmountVarName, out droppable2))
			{
				if (droppable2 == null)
				{
					Diagnostics.LogError("QuestDropListVariableDefinition is null, quest variable (varname: '{0}')", new object[]
					{
						this.AmountVarName
					});
					return false;
				}
				DroppableInteger droppableInteger = droppable2 as DroppableInteger;
				if (droppableInteger == null)
				{
					Diagnostics.LogError("QuestDropListVariableDefinition does not contains a DroppableInteger (varname: '{0}')", new object[]
					{
						this.AmountVarName
					});
					return false;
				}
				this.Amount = droppableInteger.Value;
			}
			else
			{
				float f;
				if (!questBehaviour.TryGetQuestVariableValueByName<float>(this.AmountVarName, out f))
				{
					Diagnostics.LogError("Cannot retrieve quest variable (varname: '{0}')", new object[]
					{
						this.AmountVarName
					});
					return false;
				}
				this.Amount = Mathf.RoundToInt(f);
			}
		}
		return base.Initialize(questBehaviour);
	}
}
