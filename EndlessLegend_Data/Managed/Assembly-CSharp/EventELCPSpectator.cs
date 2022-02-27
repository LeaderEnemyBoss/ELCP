using System;
using Amplitude;
using Amplitude.Unity.Event;

public class EventELCPSpectator : Event
{
	public EventELCPSpectator() : base(EventELCPSpectator.Name, new object[0])
	{
	}

	public static StaticString Name = "EventELCPSpectator";
}
