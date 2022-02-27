using System;
using System.Collections.Generic;
using Amplitude.Path;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.SimulationModifierDescriptors;
using UnityEngine;

namespace Amplitude.Unity.Gui.SimulationEffect
{
	public class SimulationEffectParser
	{
		public IGuiService GuiService { get; set; }

		public List<StaticString> LocalDescriptorTypeException { get; set; }

		public void GetScope(SimulationPath path, out string to)
		{
			to = string.Empty;
			if (0 < path.Length)
			{
				int num = 1;
				if (path.ContainsAt(Path<SimulationObject>.Parent, 0) || path.ContainsAt(Path<SimulationObject>.FirstParent, 0) || path.ContainsAt(Path<SimulationObject>.Root, 0))
				{
					num++;
				}
				this.GetScope(path[path.Length - 1], out to, num < path.Length);
			}
			else
			{
				to = "%PathEmpty";
			}
		}

		public void GetScope(PathTokens pathTarget, out string to, bool isMultiple = false)
		{
			to = string.Empty;
			for (int i = pathTarget.Length - 1; i >= 0; i--)
			{
				int byteIndex = i / 8;
				int bitIndex = i % 8;
				if (!pathTarget.GetGlobalBit(bitIndex, byteIndex))
				{
					to = pathTarget[i];
					break;
				}
			}
			if (string.IsNullOrEmpty(to))
			{
				to = pathTarget[pathTarget.Length - 1];
			}
			if (isMultiple)
			{
				string text = this.ComputeReadableString(to + "Multiple", false);
				if (!string.IsNullOrEmpty(text))
				{
					to = text;
				}
				else
				{
					to = this.ComputeReadableString(to, true);
				}
			}
			else
			{
				to = this.ComputeReadableString(to, true);
			}
		}

		public void ParseSimulationDescriptor(SimulationDescriptor descriptor, List<EffectDescription> effectDescriptions, StaticString defaultClass, bool displayEmptyDescriptors = false, SimulationObject context = null, bool isContextTheSource = true, bool isForceOn = false, bool parseTitle = false)
		{
			if (descriptor == null)
			{
				throw new ArgumentNullException("descriptor");
			}
			GuiElement guiElement;
			if (this.GuiService.GuiPanelHelper.TryGetGuiElement(descriptor.Name, out guiElement) && guiElement is ExtendedGuiElement)
			{
				ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
				if (extendedGuiElement.TooltipElement != null)
				{
					if (!string.IsNullOrEmpty(extendedGuiElement.TooltipElement.EffectOverride))
					{
						EffectDescription effectDescription = new EffectDescription();
						effectDescription.Override = AgeLocalizer.Instance.LocalizeString(extendedGuiElement.TooltipElement.EffectOverride);
						if (parseTitle)
						{
							effectDescription.Title = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
						}
						effectDescriptions.Add(effectDescription);
					}
					if (extendedGuiElement.TooltipElement.Ignore)
					{
						return;
					}
				}
			}
			if (descriptor.SimulationModifierDescriptors == null)
			{
				if (this.GuiService.GuiPanelHelper.TryGetGuiElement(descriptor.Name, out guiElement))
				{
					if (guiElement is ExtendedGuiElement)
					{
						ExtendedGuiElement extendedGuiElement2 = guiElement as ExtendedGuiElement;
						if (extendedGuiElement2.TooltipElement != null && extendedGuiElement2.TooltipElement.Ignore)
						{
							return;
						}
					}
					EffectDescription effectDescription2 = new EffectDescription();
					effectDescription2.Override = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
					if (parseTitle)
					{
						effectDescription2.Title = AgeLocalizer.Instance.LocalizeString(guiElement.Title);
					}
					effectDescription2.On = this.ComputeReadableString(defaultClass, false);
					effectDescriptions.Add(effectDescription2);
				}
				return;
			}
			bool flag = false;
			if (parseTitle)
			{
				flag = true;
			}
			for (int i = 0; i < descriptor.SimulationModifierDescriptors.Length; i++)
			{
				SimulationModifierDescriptor simulationModifierDescriptor = descriptor.SimulationModifierDescriptors[i];
				if (simulationModifierDescriptor.Operation != SimulationModifierDescriptor.ModifierOperation.Force && !simulationModifierDescriptor.TooltipHidden)
				{
					EffectDescription effectDescription3 = null;
					this.ParseModifier(descriptor, simulationModifierDescriptor, defaultClass, context, isContextTheSource, out effectDescription3, isForceOn);
					if (effectDescription3 != null)
					{
						if (context != null && isContextTheSource)
						{
							effectDescription3.From = this.ComputeReadableString(descriptor.Name, true);
						}
						if (flag)
						{
							effectDescription3.Title = AgeLocalizer.Instance.LocalizeString("%" + descriptor.Name + "Title");
							flag = false;
						}
						effectDescriptions.Add(effectDescription3);
					}
				}
			}
		}

		protected virtual void ParseModifier(SimulationDescriptor simulationDescriptor, SimulationModifierDescriptor modifierDescriptor, StaticString defaultClass, SimulationObject context, bool isContextSource, out EffectDescription effectMapperElementDescription, bool isForceOn = false)
		{
			effectMapperElementDescription = null;
			bool forcePercent = false;
			GuiElement guiElement;
			if (this.GuiService.GuiPanelHelper.TryGetGuiElement(modifierDescriptor.TargetPropertyName, out guiElement) && guiElement is ExtendedGuiElement)
			{
				ExtendedGuiElement extendedGuiElement = guiElement as ExtendedGuiElement;
				if (extendedGuiElement.TooltipElement != null)
				{
					if (extendedGuiElement.TooltipElement.Ignore)
					{
						return;
					}
					forcePercent = extendedGuiElement.TooltipElement.Percent;
				}
			}
			SimulationObject simulationObject = context;
			if (!isContextSource)
			{
				simulationObject = null;
			}
			if (modifierDescriptor is SingleSimulationModifierDescriptor)
			{
				effectMapperElementDescription = this.ParseSingleModifier(simulationDescriptor, modifierDescriptor as SingleSimulationModifierDescriptor, simulationObject, forcePercent);
			}
			else if (modifierDescriptor is BinarySimulationModifierDescriptor)
			{
				effectMapperElementDescription = this.ParseBinaryModifier(simulationDescriptor, modifierDescriptor as BinarySimulationModifierDescriptor, simulationObject, forcePercent);
			}
			else if (modifierDescriptor is CountSimulationModifierDescriptor)
			{
				effectMapperElementDescription = this.ParseCountModifier(simulationDescriptor, modifierDescriptor as CountSimulationModifierDescriptor, simulationObject, forcePercent);
			}
			if (effectMapperElementDescription == null || string.IsNullOrEmpty(effectMapperElementDescription.Value))
			{
				return;
			}
			if (!string.IsNullOrEmpty(modifierDescriptor.TooltipOverride))
			{
				if (modifierDescriptor.LocalizeTooltipOverride)
				{
					effectMapperElementDescription.Format = AgeLocalizer.Instance.LocalizeString(modifierDescriptor.TooltipOverride);
				}
				else
				{
					effectMapperElementDescription.Format = modifierDescriptor.TooltipOverride;
				}
			}
			effectMapperElementDescription.Symbol = this.ComputePropertySymbol(modifierDescriptor.TargetPropertyName);
			if (simulationObject != null)
			{
			}
			if (simulationObject == null || isForceOn)
			{
				string on = string.Empty;
				if (modifierDescriptor.Path != null && modifierDescriptor.Path.Length > 0 && modifierDescriptor.Path[0].Length > 0)
				{
					this.GetScope(modifierDescriptor.Path, out on);
				}
				else
				{
					on = this.ComputeReadableString(defaultClass, true);
				}
				effectMapperElementDescription.On = on;
			}
			string condition = string.Empty;
			if (modifierDescriptor.Path != null && modifierDescriptor.Path.Length > 0 && modifierDescriptor.Path[0].Length > 0)
			{
				condition = this.ComputeDuringString(modifierDescriptor.Path);
			}
			effectMapperElementDescription.Condition = condition;
		}

		protected float ComputeOperationTypeToken(SimulationModifierDescriptor.ModifierOperation operationType, ref string operation, ref string valueFormat, ref bool round)
		{
			float result = 0f;
			round = false;
			valueFormat = "#####0.#";
			switch (operationType)
			{
			case SimulationModifierDescriptor.ModifierOperation.Addition:
				operation = "+";
				break;
			case SimulationModifierDescriptor.ModifierOperation.Subtraction:
				operation = "-";
				break;
			case SimulationModifierDescriptor.ModifierOperation.Multiplication:
				operation = "×";
				result = 1f;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Division:
				operation = "/";
				result = 1f;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Power:
				operation = "^";
				result = 1f;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Percent:
				operation = "+";
				valueFormat = "#####0%";
				break;
			case SimulationModifierDescriptor.ModifierOperation.Maximum:
				operation = "max. ";
				result = float.NegativeInfinity;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Minimum:
				operation = "min. ";
				result = float.PositiveInfinity;
				break;
			default:
				operation = string.Empty;
				break;
			}
			return result;
		}

		protected float ExecuteOperation(SimulationModifierDescriptor.ModifierOperation operationType, float left, float right)
		{
			float num = 0f;
			switch (operationType)
			{
			case SimulationModifierDescriptor.ModifierOperation.Addition:
				num = left + right;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Subtraction:
				num = left - right;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Multiplication:
				num = left * right;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Division:
				if (right != 0f)
				{
					num = left / right;
				}
				break;
			case SimulationModifierDescriptor.ModifierOperation.Power:
				num = Mathf.Pow(left, right);
				break;
			case SimulationModifierDescriptor.ModifierOperation.Percent:
				num = left + right;
				num *= 100f;
				break;
			case SimulationModifierDescriptor.ModifierOperation.Maximum:
				num = ((left >= right) ? left : right);
				break;
			case SimulationModifierDescriptor.ModifierOperation.Minimum:
				num = ((left <= right) ? left : right);
				break;
			}
			return num;
		}

		protected EffectDescription ParseSingleModifier(SimulationDescriptor simulationDescriptor, SingleSimulationModifierDescriptor modifierDescriptor, SimulationObject context = null, bool forcePercent = false)
		{
			EffectDescription effectDescription = new EffectDescription();
			bool round = false;
			string valueFormat = string.Empty;
			string empty = string.Empty;
			float neutralValue = this.ComputeOperationTypeToken(modifierDescriptor.Operation, ref empty, ref valueFormat, ref round);
			effectDescription.Operator = empty;
			object obj = modifierDescriptor.PrecomputedValue;
			if (forcePercent)
			{
				valueFormat = "#####0%";
			}
			if (obj is float)
			{
				effectDescription.ValueFloat = (float)obj;
				effectDescription.ValueFormat = valueFormat;
				effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
				if (string.IsNullOrEmpty(effectDescription.Value))
				{
					return null;
				}
				if ((float)obj < 0f && (modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Addition || modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Percent))
				{
					effectDescription.Operator = string.Empty;
				}
			}
			else if (obj is StaticString)
			{
				if (context != null)
				{
					obj = context.GetPropertyValue(obj as StaticString);
				}
				else if (simulationDescriptor.SimulationPropertyDescriptors != null)
				{
					StaticString staticString = obj as StaticString;
					for (int i = 0; i < simulationDescriptor.SimulationPropertyDescriptors.Length; i++)
					{
						if (staticString != null && simulationDescriptor.SimulationPropertyDescriptors[i].Name == staticString)
						{
							obj = simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue;
							break;
						}
					}
				}
				if (obj is StaticString)
				{
					effectDescription.Value = this.ComputePropertySymbol(obj.ToString());
				}
				else
				{
					effectDescription.ValueFloat = (float)obj;
					effectDescription.ValueFormat = valueFormat;
					effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
					if (string.IsNullOrEmpty(effectDescription.Value))
					{
						return null;
					}
				}
			}
			return effectDescription;
		}

		protected EffectDescription ParseCountModifier(SimulationDescriptor simulationDescriptor, CountSimulationModifierDescriptor modifierDescriptor, SimulationObject context = null, bool forcePercent = false)
		{
			EffectDescription effectDescription = new EffectDescription();
			bool round = false;
			string valueFormat = string.Empty;
			string empty = string.Empty;
			float neutralValue = this.ComputeOperationTypeToken(modifierDescriptor.Operation, ref empty, ref valueFormat, ref round);
			effectDescription.Operator = empty;
			if (forcePercent)
			{
				valueFormat = "#####0%";
			}
			object obj = modifierDescriptor.PrecomputedValue;
			if (obj is float)
			{
				effectDescription.ValueFloat = (float)obj;
				effectDescription.ValueFormat = valueFormat;
				effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
				if (string.IsNullOrEmpty(effectDescription.Value))
				{
					return null;
				}
				if ((float)obj < 0f && (modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Addition || modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Percent))
				{
					effectDescription.Operator = string.Empty;
				}
			}
			else if (obj is StaticString)
			{
				if (context != null)
				{
					obj = context.GetPropertyValue(obj as StaticString);
				}
				else if (simulationDescriptor.SimulationPropertyDescriptors != null)
				{
					StaticString staticString = obj as StaticString;
					for (int i = 0; i < simulationDescriptor.SimulationPropertyDescriptors.Length; i++)
					{
						if (staticString != null && simulationDescriptor.SimulationPropertyDescriptors[i].Name == staticString)
						{
							obj = simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue;
							break;
						}
					}
				}
				if (obj is StaticString)
				{
					effectDescription.Value = this.ComputePropertySymbol(obj.ToString());
				}
				else
				{
					effectDescription.ValueFloat = (float)obj;
					effectDescription.ValueFormat = valueFormat;
					effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
					if (string.IsNullOrEmpty(effectDescription.Value))
					{
						return null;
					}
				}
			}
			string empty2 = string.Empty;
			if (modifierDescriptor.CountPath != null && modifierDescriptor.CountPath.Length > 0 && modifierDescriptor.CountPath[0].Length > 0)
			{
				this.GetScope(modifierDescriptor.CountPath, out empty2);
			}
			effectDescription.By = empty2;
			return effectDescription;
		}

		protected EffectDescription ParseBinaryModifier(SimulationDescriptor simulationDescriptor, BinarySimulationModifierDescriptor modifierDescriptor, SimulationObject context = null, bool forcePercent = false)
		{
			EffectDescription effectDescription = new EffectDescription();
			bool round = false;
			string valueFormat = string.Empty;
			string empty = string.Empty;
			float neutralValue = this.ComputeOperationTypeToken(modifierDescriptor.Operation, ref empty, ref valueFormat, ref round);
			effectDescription.Operator = empty;
			if (forcePercent)
			{
				valueFormat = "#####0%";
			}
			bool flag = false;
			string empty2 = string.Empty;
			string empty3 = string.Empty;
			float num = this.ComputeOperationTypeToken(modifierDescriptor.BinaryOperation, ref empty3, ref empty2, ref flag);
			object obj = modifierDescriptor.LeftPrecomputedValue;
			object obj2 = modifierDescriptor.RightPrecomputedValue;
			if (context != null && !modifierDescriptor.IsBindOnSource)
			{
				if (obj is StaticString)
				{
					obj = context.GetPropertyValue(obj as StaticString);
				}
				if (obj2 is StaticString)
				{
					obj2 = context.GetPropertyValue(obj2 as StaticString);
				}
			}
			else if (simulationDescriptor.SimulationPropertyDescriptors != null)
			{
				StaticString staticString = obj as StaticString;
				StaticString staticString2 = obj2 as StaticString;
				for (int i = 0; i < simulationDescriptor.SimulationPropertyDescriptors.Length; i++)
				{
					if (staticString != null && simulationDescriptor.SimulationPropertyDescriptors[i].Name == staticString && simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue != 0f)
					{
						obj = simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue;
						staticString = null;
					}
					if (staticString2 != null && simulationDescriptor.SimulationPropertyDescriptors[i].Name == staticString2 && simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue != 0f)
					{
						obj2 = simulationDescriptor.SimulationPropertyDescriptors[i].BaseValue;
						staticString2 = null;
					}
				}
			}
			if (context != null && modifierDescriptor.FindStringValueForParserCalculation)
			{
				if (obj is StaticString)
				{
					obj = context.GetPropertyValue(obj as StaticString);
				}
				if (obj2 is StaticString)
				{
					obj2 = context.GetPropertyValue(obj2 as StaticString);
				}
			}
			if (obj is float && obj2 is float)
			{
				float num2 = this.ExecuteOperation(modifierDescriptor.BinaryOperation, (float)obj, (float)obj2);
				if (num2 < 0f && (modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Addition || modifierDescriptor.Operation == SimulationModifierDescriptor.ModifierOperation.Percent))
				{
					effectDescription.Operator = string.Empty;
				}
				effectDescription.ValueFloat = num2;
				effectDescription.ValueFormat = valueFormat;
				effectDescription.Value = this.ComputeReadableFloat(num2, round, valueFormat, neutralValue);
			}
			else if (modifierDescriptor.BinaryOperation == SimulationModifierDescriptor.ModifierOperation.Multiplication && (obj is StaticString || obj2 is StaticString))
			{
				if (obj is StaticString && obj2 is float)
				{
					obj = obj2;
					obj2 = modifierDescriptor.LeftPrecomputedValue;
				}
				if (obj2 is StaticString)
				{
					effectDescription.Per = this.ComputePropertySymbol(obj2 as StaticString);
				}
				if (obj is float)
				{
					effectDescription.ValueFloat = (float)obj;
					effectDescription.ValueFormat = valueFormat;
					effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
				}
				else if (obj is StaticString)
				{
					effectDescription.Value = this.ComputePropertySymbol(obj as StaticString);
				}
			}
			else if (obj is float)
			{
				effectDescription.ValueFloat = (float)obj;
				effectDescription.ValueFormat = valueFormat;
				effectDescription.Value = this.ComputeReadableFloat((float)obj, round, valueFormat, neutralValue);
			}
			else
			{
				effectDescription.Value = string.Concat(new string[]
				{
					obj.ToString(),
					" ",
					empty3,
					" ",
					obj2.ToString()
				});
			}
			if (string.IsNullOrEmpty(effectDescription.Value))
			{
				return null;
			}
			return effectDescription;
		}

		protected string ComputePropertySymbol(string propertyName)
		{
			string empty = string.Empty;
			if (!this.GuiService.TryFormatSymbol(propertyName, out empty, true))
			{
				return this.ComputeReadableString(propertyName, true);
			}
			return empty;
		}

		protected string ComputeReadableFloat(float floatValue, bool round, string valueFormat, float neutralValue)
		{
			if (round)
			{
				floatValue = Mathf.Round(floatValue);
			}
			if (floatValue == neutralValue)
			{
				return string.Empty;
			}
			if (floatValue < 0f)
			{
			}
			return floatValue.ToString(valueFormat);
		}

		protected string ComputeReadableString(string guiName, bool allowDefault = true)
		{
			GuiElement guiElement = null;
			if (this.GuiService.GuiPanelHelper.TryGetGuiElement(guiName, out guiElement))
			{
				return AgeLocalizer.Instance.LocalizeString(guiElement.Title);
			}
			if (allowDefault)
			{
				return AgeLocalizer.Instance.LocalizeString("%" + guiName + "Title");
			}
			return AgeLocalizer.Instance.LocalizeStringDefaults("%" + guiName + "Title", string.Empty);
		}

		protected string ComputeDuringString(SimulationPath path)
		{
			for (int i = 0; i < path.Length; i++)
			{
				for (int j = 0; j < path[i].Length; j++)
				{
					int byteIndex = j / 8;
					int bitIndex = j % 8;
					if (path[i].GetGlobalBit(bitIndex, byteIndex))
					{
						return this.ComputeReadableString(path[i][j], true);
					}
				}
			}
			return string.Empty;
		}

		public const string ValueFormat = "#####0.#";

		public const string PercentFormat = "#####0%";

		public const string CompoundLocalizer = "Multiple";
	}
}
