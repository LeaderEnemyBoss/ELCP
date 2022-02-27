using System;
using System.Xml.Serialization;

namespace Amplitude.Unity.Simulation.Advanced
{
	public class InterpreterPrerequisite : Prerequisite
	{
		public InterpreterPrerequisite()
		{
			this.initialValue = string.Empty;
			base..ctor();
		}

		[XmlText]
		public string XmlSerializableInterperterValue
		{
			get
			{
				return this.initialValue;
			}
			set
			{
				this.initialValue = value;
				this.interpreterTokens = Interpreter.InfixTransform(value);
			}
		}

		public override bool Check(SimulationObject simulationObject)
		{
			if (simulationObject == null)
			{
				throw new ArgumentNullException("simulationObject");
			}
			bool result;
			using (InterpreterContext.InterpreterSession interpreterSession = new InterpreterContext.InterpreterSession(simulationObject))
			{
				result = this.Check(interpreterSession.Context);
			}
			return result;
		}

		public override bool Check(InterpreterContext context)
		{
			if (this.interpreterTokens == null)
			{
				Diagnostics.Log("The interpreter prerequisite has no token. InitialValue={0}.", new object[]
				{
					this.initialValue
				});
				return true;
			}
			object obj = Interpreter.Execute(this.interpreterTokens, context);
			if (obj == null)
			{
				return base.Inverted;
			}
			if (!(obj is bool))
			{
				try
				{
					bool flag = (bool)Convert.ChangeType(obj, typeof(bool));
					return flag ^ base.Inverted;
				}
				catch
				{
					Diagnostics.LogWarning("The interpreter prerequisite has returned a non-boolean convertible value (InitialValue = '{0}').", new object[]
					{
						this.initialValue
					});
					return false;
				}
			}
			return (bool)obj ^ base.Inverted;
		}

		public override string ToString()
		{
			return this.initialValue;
		}

		public InterpreterPrerequisite(string InterperterValue, bool inverted, params string[] flags) : base(inverted, flags)
		{
			this.XmlSerializableInterperterValue = InterperterValue;
		}

		private object[] interpreterTokens;

		private string initialValue;
	}
}
