using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Simulation.Advanced;
using UnityEngine;

public class InfiltrationActionOnEmpire_StealResource : InfiltrationActionOnEmpire
{
	[XmlElement(ElementName = "EmpireResource")]
	public string ResourceName { get; private set; }

	[XmlElement(ElementName = "StealParameters")]
	public InfiltrationActionOnEmpire_StealResource.StealResourcesAmountParameters AmountParameters { get; private set; }

	public override bool CanExecute(InterpreterContext context, ref List<StaticString> failureFlags, params object[] parameters)
	{
		if (!base.CanExecute(context, ref failureFlags, parameters))
		{
			return false;
		}
		if (context == null)
		{
			failureFlags.Add(InfiltrationAction.NoCanDoWhileSystemError);
			return false;
		}
		if (!context.Contains("InfiltratedCity"))
		{
			failureFlags.Add(InfiltrationAction.NoCanDoWhileHidden);
			return false;
		}
		City city = context.Get("InfiltratedCity") as City;
		if (city == null)
		{
			failureFlags.Add(InfiltrationAction.NoCanDoWhileSystemError);
			return false;
		}
		DepartmentOfTheTreasury agency = city.Empire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		if (!agency.TryGetResourceStockValue(city.Empire.SimulationObject, this.ResourceName, out num, false))
		{
			failureFlags.Add(InfiltrationAction.NoCanDoWhileSystemError);
			return false;
		}
		if (num <= 0f)
		{
			string x = InfiltrationActionOnEmpire_StealResource.NoCanDoWhileNoResourceToSteal + this.ResourceName;
			failureFlags.Add(x);
			return false;
		}
		return true;
	}

	public override bool Execute(City infiltratedCity, PlayerController playerController, out Ticket ticket, EventHandler<TicketRaisedEventArgs> ticketRaisedEventHandler, params object[] parameters)
	{
		ticket = null;
		if (playerController != null)
		{
			float num = this.ComputeAmountToSteal(infiltratedCity.Empire);
			OrderTransferResourcesByInfiltration order = new OrderTransferResourcesByInfiltration(playerController.Empire.Index, this.ResourceName, -num, infiltratedCity.GUID, this.Name);
			playerController.PostOrder(order);
			OrderTransferResources order2 = new OrderTransferResources(playerController.Empire.Index, this.ResourceName, num, 0UL);
			playerController.PostOrder(order2);
			return true;
		}
		return false;
	}

	private float ComputeAmountToSteal(Empire targetEmpire)
	{
		DepartmentOfTheTreasury agency = targetEmpire.GetAgency<DepartmentOfTheTreasury>();
		float num = 0f;
		if (agency.TryGetResourceStockValue(targetEmpire.SimulationObject, this.ResourceName, out num, false))
		{
			float num2 = this.AmountParameters.BaseAmount * this.AmountParameters.RandomThreshold;
			float num3 = num * this.AmountParameters.TargetStockPercentage;
			num3 += UnityEngine.Random.Range(this.AmountParameters.BaseAmount - num2, this.AmountParameters.BaseAmount + num2);
			return (float)Math.Floor((double)Math.Min(num3, num));
		}
		return 0f;
	}

	protected static StaticString NoCanDoWhileNoResourceToSteal = "InfiltrationActionNoResourceToSteal";

	[StructLayout(LayoutKind.Sequential, Size = 1)]
	public struct StealResourcesAmountParameters
	{
		[XmlAttribute]
		public float BaseAmount { get; set; }

		[XmlAttribute]
		public float RandomThreshold { get; set; }

		[XmlAttribute]
		public float TargetStockPercentage { get; set; }
	}
}
