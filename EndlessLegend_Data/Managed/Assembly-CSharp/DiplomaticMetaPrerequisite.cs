using System;
using System.Xml.Serialization;
using Amplitude;

public class DiplomaticMetaPrerequisite : DiplomaticPrerequisite
{
	[XmlAttribute("Operator")]
	public DiplomaticMetaPrerequisite.OperatorType Operator { get; private set; }

	[XmlElement(Type = typeof(DiplomaticRelationStateEmpirePrerequisite), ElementName = "DiplomaticRelationStateEmpirePrerequisite")]
	[XmlElement(Type = typeof(DiplomaticMetaPrerequisite), ElementName = "DiplomaticMetaPrerequisite")]
	[XmlElement(Type = typeof(DiplomaticRelationStatePrerequisite), ElementName = "DiplomaticRelationStatePrerequisite")]
	[XmlElement(Type = typeof(DiplomaticContractContainsTermPrerequisite), ElementName = "DiplomaticContractContainsTermPrerequisite")]
	[XmlElement(Type = typeof(DiplomaticAbilityPrerequisite), ElementName = "DiplomaticAbilityPrerequisite")]
	[XmlElement(Type = typeof(DiplomaticCooldownPrerequisite), ElementName = "DiplomaticCooldownPrerequisite")]
	public DiplomaticPrerequisite[] Prerequisites { get; private set; }

	public override bool Check(IDiplomaticContract diplomaticContract, Empire empireWhichProvides, Empire empireWhichReceives)
	{
		Diagnostics.Assert(this.Prerequisites != null && this.Prerequisites.Length != 0);
		int num = 0;
		for (int i = 0; i < this.Prerequisites.Length; i++)
		{
			if (this.Prerequisites[i].Check(diplomaticContract, empireWhichProvides, empireWhichReceives))
			{
				num++;
			}
		}
		bool flag;
		switch (this.Operator)
		{
		case DiplomaticMetaPrerequisite.OperatorType.OR:
			flag = (num > 0);
			break;
		case DiplomaticMetaPrerequisite.OperatorType.XOR:
			flag = (num == 1);
			break;
		case DiplomaticMetaPrerequisite.OperatorType.NOR:
			flag = (num == 0);
			break;
		case DiplomaticMetaPrerequisite.OperatorType.AND:
			flag = (num == this.Prerequisites.Length);
			break;
		case DiplomaticMetaPrerequisite.OperatorType.NAND:
			flag = (num < this.Prerequisites.Length);
			break;
		default:
			throw new ArgumentOutOfRangeException("Operator");
		}
		return base.Inverted ^ flag;
	}

	public enum OperatorType
	{
		OR,
		XOR,
		NOR,
		AND,
		NAND
	}
}
