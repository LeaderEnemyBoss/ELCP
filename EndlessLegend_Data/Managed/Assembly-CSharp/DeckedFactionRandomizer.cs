using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Extensions;

public class DeckedFactionRandomizer : FactionRandomizer
{
	public DeckedFactionRandomizer(Session session) : base(session)
	{
		this.deck = this.factions.ToList<Faction>().Randomize(null);
	}

	public override Faction Next()
	{
		if (this.deck.Count == 0)
		{
			this.deck = this.factions.ToList<Faction>().Randomize(null);
		}
		int index = this.deck.Count - 1;
		Faction result = this.deck[index];
		this.deck.RemoveAt(index);
		return result;
	}

	internal override void AddRef(Faction faction)
	{
		this.deck.RemoveAll((Faction item) => item.Name == faction.Name);
	}

	private List<Faction> deck = new List<Faction>();
}
