using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using Amplitude.Query;
using Amplitude.Query.Xml;
using Amplitude.Unity.Simulation;
using Amplitude.Unity.Simulation.Advanced;

public class QuestVariableDefinition
{
	[XmlAttribute]
	public string VarName { get; set; }

	[XmlAttribute]
	public int Value { get; set; }

	[XmlAttribute]
	public bool FromGlobal { get; set; }

	[XmlAttribute]
	public bool UsedInPrerequisites { get; set; }

	[XmlElement("Count", typeof(XmlQueryCount))]
	[XmlElement("Any", typeof(XmlQueryAny))]
	[XmlElement("Limit", typeof(XmlQueryLimit))]
	[XmlElement("Last", typeof(XmlQueryLast))]
	[XmlElement("From", typeof(XmlQuery))]
	[XmlElement("First", typeof(XmlQueryFirst))]
	public IQuery Query { get; set; }

	public static InterpreterContext.InterpreterSession CreateSession(SimulationObjectWrapper simObject, IEnumerable<QuestVariable> variables)
	{
		InterpreterContext.InterpreterSession result = new InterpreterContext.InterpreterSession(simObject);
		if (variables != null)
		{
			foreach (QuestVariable questVariable in variables)
			{
				if (questVariable.Object is float)
				{
					result.Context.Register(questVariable.Name, (float)Convert.ChangeType(questVariable.Object, typeof(float)));
				}
				else if (questVariable.Object is IList)
				{
					IList list = (IList)questVariable.Object;
					if (list.Count == 1)
					{
						object obj = list[0];
						if (obj is float)
						{
							result.Context.Register(questVariable.Name, (float)Convert.ChangeType(obj, typeof(float)));
						}
					}
				}
			}
		}
		return result;
	}

	[XmlAttribute]
	public bool ToGlobal { get; set; }
}
