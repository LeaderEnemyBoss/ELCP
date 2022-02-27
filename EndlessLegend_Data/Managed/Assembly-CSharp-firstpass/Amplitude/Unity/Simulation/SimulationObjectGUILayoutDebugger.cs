using System;
using System.Collections.Generic;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Simulation.SimulationModifierDescriptors;
using Amplitude.Unity.UI;
using UnityEngine;

namespace Amplitude.Unity.Simulation
{
	public class SimulationObjectGUILayoutDebugger
	{
		public SimulationObjectGUILayoutDebugger(bool fancyLayout)
		{
			this.isFancyLayoutMode = fancyLayout;
			this.horizontalStyle = new GUIStyle();
			RectOffset padding = this.horizontalStyle.padding;
			int num = 0;
			this.horizontalStyle.padding.bottom = num;
			num = num;
			this.horizontalStyle.padding.top = num;
			num = num;
			this.horizontalStyle.padding.right = num;
			padding.left = num;
			RectOffset margin = this.horizontalStyle.margin;
			num = 0;
			this.horizontalStyle.margin.bottom = num;
			num = num;
			this.horizontalStyle.margin.top = num;
			num = num;
			this.horizontalStyle.margin.right = num;
			margin.left = num;
			this.horizontalLabelStyle = new GUIStyle();
			this.horizontalLabelStyle.normal.textColor = Color.white;
			RectOffset padding2 = this.horizontalLabelStyle.padding;
			num = 0;
			this.horizontalLabelStyle.padding.bottom = num;
			num = num;
			this.horizontalLabelStyle.padding.top = num;
			num = num;
			this.horizontalLabelStyle.padding.right = num;
			padding2.left = num;
			RectOffset margin2 = this.horizontalLabelStyle.margin;
			num = 0;
			this.horizontalLabelStyle.margin.bottom = num;
			num = num;
			this.horizontalLabelStyle.margin.top = num;
			num = num;
			this.horizontalLabelStyle.margin.right = num;
			margin2.left = num;
			this.horizontalLabelStyle.wordWrap = true;
			this.horizontalLabelStyle.stretchWidth = true;
			this.horizontalLabelStyle.alignment = TextAnchor.MiddleLeft;
		}

		public void OnGUI(SimulationObject simulationObject, Color simulationObjectNameColor)
		{
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
			{
				this.DisplaySimulationObject(5f, simulationObject, simulationObjectNameColor);
			}
		}

		private void DisplaySimulationObject(float indent, SimulationObject simulationObject, Color simulationObjectNameColor)
		{
			string text = simulationObject.Name;
			if (simulationObject.DebugInfo == null)
			{
				simulationObject.DebugInfo = new SimulationObjectDebugInfo(simulationObject);
			}
			Color color = GUI.color;
			if (indent == 5f)
			{
				this.IndentedLabel(indent, "<b>Global Tags:</b>", " {0}", new object[]
				{
					SimulationGlobal.GetGlobalTags().ToString()
				});
			}
			GUI.color = simulationObjectNameColor;
			simulationObject.DebugInfo.DisplayContent = this.IndentedToggle(indent, simulationObject.DebugInfo.DisplayContent, "{0}", new object[]
			{
				text
			});
			GUI.color = color;
			if (simulationObject.DebugInfo.DisplayContent)
			{
				indent += 20f;
				this.IndentedLabel(indent, "<b>Tags:</b>", " {0}", new object[]
				{
					simulationObject.Tags.ToString()
				});
				if (simulationObject.IsPropertiesDirty)
				{
					this.IndentedLabel(indent, "<color=red>Dirty</color>", new object[0]);
				}
				if (simulationObject.Properties != null && simulationObject.Properties.Count > 0)
				{
					simulationObject.DebugInfo.DisplayProperties = this.IndentedToggle(indent, simulationObject.DebugInfo.DisplayProperties, "<b>Properties</b>", new object[0]);
					if (simulationObject.DebugInfo.DisplayProperties)
					{
						indent += 20f;
						this.propertiesCache.Clear();
						simulationObject.Properties.Fill(ref this.propertiesCache);
						this.propertiesCache.Sort((SimulationProperty left, SimulationProperty right) => left.Name.CompareTo(right.Name));
						for (int i = 0; i < this.propertiesCache.Count; i++)
						{
							this.DisplayProperty(indent, this.propertiesCache[i], simulationObject);
						}
						indent -= 20f;
					}
				}
				if (simulationObject.Children.Count > 0)
				{
					UnityEngine.GUILayout.BeginHorizontal(new GUILayoutOption[0]);
					UnityEngine.GUILayout.Space(indent);
					simulationObject.DebugInfo.DisplayChildren = UnityEngine.GUILayout.Toggle(simulationObject.DebugInfo.DisplayChildren, "<b>Children</b>", new GUILayoutOption[0]);
					UnityEngine.GUILayout.EndHorizontal();
					if (simulationObject.DebugInfo.DisplayChildren)
					{
						indent += 20f;
						List<SimulationObject> list = new List<SimulationObject>(simulationObject.Children);
						list.Sort(delegate(SimulationObject left, SimulationObject right)
						{
							string text2 = left.Name;
							string strB = right.Name;
							return text2.CompareTo(strB);
						});
						for (int j = 0; j < list.Count; j++)
						{
							this.DisplaySimulationObject(indent, simulationObject.Children[j], GUI.color);
						}
						indent -= 20f;
					}
				}
			}
		}

		private void DisplayProperty(float indent, SimulationProperty property, SimulationObject context)
		{
			int propertyIndex = context.GetPropertyIndex(property.Name);
			context.DebugInfo.SetPropertyContent(propertyIndex, this.IndentedToggle(indent, context.DebugInfo.DisplayPropertyContent(propertyIndex), "{0} = <color=orange>{1}</color> {2}", new object[]
			{
				property.Name,
				property.Value,
				(!context.IsPropertyDirty(property.Name)) ? string.Empty : "<size=11><color=red>(Dirty)</color></size>"
			}));
			if (context.DebugInfo.DisplayPropertyContent(propertyIndex))
			{
				indent += 20f;
				UnityEngine.GUILayout.BeginVertical(new GUILayoutOption[0]);
				this.IndentedLabel(indent, "<size=11><color=grey>Index: <b>{0}</b></color></size>", new object[]
				{
					context.GetPropertyIndex(property.Name)
				});
				this.IndentedLabel(indent, "BaseValue:", " <b>{0}</b>", new object[]
				{
					property.BaseValue
				});
				this.IndentedLabel(indent, "Value:", " <b>{0}</b>", new object[]
				{
					property.Value
				});
				if (property.InternalPropertyDescriptor.Composition != SimulationPropertyComposition.None)
				{
					this.IndentedLabel(indent, "Composition:", " <b>{0}</b>", new object[]
					{
						property.PropertyDescriptor.Composition.ToString()
					});
				}
				string text = (property.PropertyDescriptor.MinValue != float.MinValue) ? ("[" + property.PropertyDescriptor.MinValue.ToString("F")) : "]-inf";
				string text2 = (property.PropertyDescriptor.MaxValue != float.MaxValue) ? (property.PropertyDescriptor.MaxValue.ToString("F") + "]") : "inf[";
				this.IndentedLabel(indent, "Range:", " <b>{0},{1}</b>", new object[]
				{
					text,
					text2
				});
				UnityEngine.GUILayout.EndVertical();
				Diagnostics.Assert(property.ModifierProviders != null);
				if (property.ModifierProviders.Count > 0)
				{
					context.DebugInfo.SetPropertyModifier(propertyIndex, this.IndentedToggle(indent, context.DebugInfo.DisplayPropertyModifier(propertyIndex), "<b>Modifiers</b>", new object[0]));
					if (context.DebugInfo.DisplayPropertyModifier(propertyIndex))
					{
						indent += 20f;
						for (int i = 0; i < property.ModifierProviders.Count; i++)
						{
							this.DisplayModifier(indent, property.ModifierProviders.Data[i], context);
						}
						indent -= 20f;
					}
				}
				indent -= 20f;
			}
		}

		private void DisplayModifier(float indent, SimulationModifierProvider provider, SimulationObject context)
		{
			string text = string.Empty;
			if (provider.ModifierDescriptor is SingleSimulationModifierDescriptor)
			{
				SingleSimulationModifierDescriptor singleSimulationModifierDescriptor = provider.ModifierDescriptor as SingleSimulationModifierDescriptor;
				text = text + provider.ModifierDescriptor.Operation.ToString() + " ";
				text += singleSimulationModifierDescriptor.Value;
			}
			else if (provider.ModifierDescriptor is BinarySimulationModifierDescriptor)
			{
				BinarySimulationModifierDescriptor binarySimulationModifierDescriptor = provider.ModifierDescriptor as BinarySimulationModifierDescriptor;
				text += string.Format("{0} ({1} {2} {3})", new object[]
				{
					provider.ModifierDescriptor.Operation.ToString(),
					binarySimulationModifierDescriptor.Left,
					binarySimulationModifierDescriptor.BinaryOperation.ToString(),
					binarySimulationModifierDescriptor.Right
				});
			}
			else if (provider.ModifierDescriptor is CountSimulationModifierDescriptor)
			{
				CountSimulationModifierDescriptor countSimulationModifierDescriptor = provider.ModifierDescriptor as CountSimulationModifierDescriptor;
				text += string.Format("{0} ({1} {2})", provider.ModifierDescriptor.Operation.ToString(), countSimulationModifierDescriptor.Value, countSimulationModifierDescriptor.XmlSerializableCountPath);
			}
			provider.DisplayContent = this.IndentedToggle(indent, provider.DisplayContent, text, new object[0]);
			if (provider.DisplayContent)
			{
				indent += 20f;
				this.IndentedLabel(indent, "Target:", " <b>{0}</b>", new object[]
				{
					provider.ModifierDescriptor.TargetPropertyName
				});
				this.IndentedLabel(indent, "Operation:", " <b>{0}</b>", new object[]
				{
					provider.ModifierDescriptor.Operation.ToString()
				});
				this.IndentedLabel(indent, "Value:", " <b>{0}</b>", new object[]
				{
					provider.ModifierDescriptor.ComputeValue(context, provider.DescriptorHolder.Context, SimulationPropertyRefreshContext.GetContext(-1)).ToString()
				});
				this.IndentedLabel(indent, "Path:", " <b>{0}</b>", new object[]
				{
					provider.ModifierDescriptor.Path.ToString()
				});
				this.IndentedLabel(indent, "Source:", " <b>{0} ({1})</b>", new object[]
				{
					provider.DescriptorHolder.Context.Name,
					provider.DescriptorHolder.Descriptor.Name
				});
				indent -= 20f;
			}
		}

		private string FormatLabelContent(string content, params object[] parameters)
		{
			return Amplitude.Unity.UI.GUILayout.Format(!this.isFancyLayoutMode, Amplitude.Unity.UI.GUILayout.FloatFormat.Default, content, parameters);
		}

		private bool IndentedToggle(float indent, bool value, string content, params object[] parameters)
		{
			UnityEngine.GUILayout.BeginHorizontal(this.horizontalStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(indent);
			value = UnityEngine.GUILayout.Toggle(value, this.FormatLabelContent(content, parameters), new GUILayoutOption[0]);
			UnityEngine.GUILayout.EndHorizontal();
			return value;
		}

		private void IndentedLabel(float indent, string content, params object[] parameters)
		{
			UnityEngine.GUILayout.BeginHorizontal(this.horizontalStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(indent);
			UnityEngine.GUILayout.Label(this.FormatLabelContent(content, parameters), this.horizontalLabelStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.EndHorizontal();
		}

		private void IndentedLabel(float indent, string title, string content, params object[] parameters)
		{
			UnityEngine.GUILayout.BeginHorizontal(this.horizontalStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.Space(indent);
			UnityEngine.GUILayout.Label(title, this.horizontalLabelStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.Label(this.FormatLabelContent(content, parameters), this.horizontalLabelStyle, new GUILayoutOption[0]);
			UnityEngine.GUILayout.FlexibleSpace();
			UnityEngine.GUILayout.EndHorizontal();
		}

		private const float IndentWidth = 20f;

		private bool isFancyLayoutMode;

		private GUIStyle horizontalStyle;

		private GUIStyle horizontalLabelStyle;

		private List<SimulationProperty> propertiesCache = new List<SimulationProperty>();
	}
}
