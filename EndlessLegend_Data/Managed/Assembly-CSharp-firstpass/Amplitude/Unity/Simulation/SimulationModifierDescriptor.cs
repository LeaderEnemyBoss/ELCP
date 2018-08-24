using System;
using System.Xml.Serialization;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

namespace Amplitude.Unity.Simulation
{
	public abstract class SimulationModifierDescriptor
	{
		public SimulationModifierDescriptor()
		{
			this.IsBase = false;
			this.IsBindOnSource = true;
			this.Operation = SimulationModifierDescriptor.ModifierOperation.Addition;
			this.Path = new SimulationPath(string.Empty);
			this.Priority = 0f;
			this.FindStringValueForParserCalculation = false;
			this.ModifierId = SimulationModifierDescriptor.nextId++;
		}

		[XmlAttribute]
		public bool IsBase
		{
			get
			{
				return this.isBase;
			}
			private set
			{
				this.isBase = value;
			}
		}

		[XmlAttribute]
		public bool IsBindOnSource
		{
			get
			{
				return this.isBindOnSource;
			}
			private set
			{
				this.isBindOnSource = value;
			}
		}

		[XmlAttribute("Operation")]
		public SimulationModifierDescriptor.ModifierOperation Operation
		{
			get
			{
				return this.operation;
			}
			private set
			{
				this.operation = value;
			}
		}

		[XmlIgnore]
		public SimulationPath Path
		{
			get
			{
				return this.path;
			}
			private set
			{
				this.path = value;
			}
		}

		[XmlAttribute("Priority")]
		public float Priority { get; private set; }

		[XmlIgnore]
		public StaticString TargetPropertyName { get; private set; }

		[XmlAttribute]
		public bool FindStringValueForParserCalculation { get; set; }

		[XmlAttribute]
		public string TooltipOverride { get; set; }

		[XmlAttribute]
		public bool LocalizeTooltipOverride { get; set; }

		[XmlAttribute]
		public bool TooltipHidden { get; set; }

		[XmlAttribute]
		public bool ValueMustBePositive { get; private set; }

		[XmlAttribute("Path")]
		public string XmlSerializableModifierPath
		{
			get
			{
				return this.Path.ToString();
			}
			private set
			{
				this.Path = new SimulationPath(value);
			}
		}

		[XmlAttribute("TargetProperty")]
		public string XmlSerializableTargetProperty
		{
			get
			{
				return this.TargetPropertyName.ToString();
			}
			private set
			{
				this.TargetPropertyName = new StaticString(value);
			}
		}

		internal int ModifierId { get; private set; }

		public abstract float ComputeValue(SimulationObject context, SimulationObject source, SimulationPropertyRefreshContext propertyRefreshContext);

		internal float Process(SimulationPropertyRefreshContext propertyRefreshContext, SimulationModifierProvider provider, SimulationObject target)
		{
			float num = 0f;
			if (this.ValueMustBePositive && propertyRefreshContext.InternalCurrentValue < 0f)
			{
				return 0f;
			}
			float num2 = this.ComputeValue(target, provider.InternalDescriptorHolder.Context, propertyRefreshContext);
			float num3 = propertyRefreshContext.InternalCurrentValue;
			switch (this.Operation)
			{
			case SimulationModifierDescriptor.ModifierOperation.Force:
				num = num2;
				propertyRefreshContext.InternalCurrentValue = num2;
				propertyRefreshContext.InternalPercentBaseValue = num2;
				num3 = 0f;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Addition:
				num = propertyRefreshContext.InternalCurrentValue + num2;
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Subtraction:
				num = propertyRefreshContext.InternalCurrentValue - num2;
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Multiplication:
				num = propertyRefreshContext.InternalCurrentValue * num2;
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Division:
				if (num2 != 0f)
				{
					num = propertyRefreshContext.InternalCurrentValue / num2;
					propertyRefreshContext.InternalPercentBaseValue = num;
					propertyRefreshContext.InternalCurrentValue = num;
				}
				break;
			case SimulationModifierDescriptor.ModifierOperation.Power:
				num = Mathf.Pow(propertyRefreshContext.InternalCurrentValue, num2);
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Percent:
				num = propertyRefreshContext.InternalCurrentValue + propertyRefreshContext.InternalPercentBaseValue * num2;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Maximum:
				num = ((propertyRefreshContext.InternalCurrentValue >= num2) ? propertyRefreshContext.InternalCurrentValue : num2);
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Minimum:
				num = ((propertyRefreshContext.InternalCurrentValue <= num2) ? propertyRefreshContext.InternalCurrentValue : num2);
				propertyRefreshContext.InternalPercentBaseValue = num;
				propertyRefreshContext.InternalCurrentValue = num;
				break;
			}
			return num - num3;
		}

		internal virtual bool ForEachPropertyOperand(SimulationModifierDescriptor.ForEachPropertyOperandFunction callback, SimulationObject owner, SimulationModifierProvider provider)
		{
			return true;
		}

		internal virtual bool NeedTagRefresh(SimulationObject owner, SimulationModifierProvider provider)
		{
			return false;
		}

		internal virtual void FillListWithOperand(FastList_OperandReference operands)
		{
		}

		internal virtual void ClearListWithOperand(FastList_OperandReference operands)
		{
		}

		protected float GetValue(object value, SimulationObject context, SimulationObject modifierSource, SimulationPropertyRefreshContext propertyRefreshContext)
		{
			if (value is float)
			{
				return (float)value;
			}
			if (!(value is StaticString))
			{
				return 0f;
			}
			if (this.IsBindOnSource)
			{
				modifierSource.Unsafe_PropertyRefresh(value as StaticString, propertyRefreshContext.InternalRequestId);
				return modifierSource.Unsafe_GetPropertyValue(value as StaticString);
			}
			context.Unsafe_PropertyRefresh(value as StaticString, propertyRefreshContext.InternalRequestId);
			return context.Unsafe_GetPropertyValue(value as StaticString);
		}

		protected bool isBindOnSource;

		private static int nextId;

		private bool isBase;

		private SimulationModifierDescriptor.ModifierOperation operation;

		private SimulationPath path;

		public enum ModifierOperation
		{
			Force,
			Addition,
			Subtraction,
			Multiplication,
			Division,
			Power,
			Percent,
			Maximum,
			Minimum
		}

		internal delegate bool ForEachPropertyOperandFunction(StaticString dependancyName, SimulationObject owner, SimulationModifierProvider provider);
	}
}
