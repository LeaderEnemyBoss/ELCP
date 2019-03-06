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
		Faction faction = this.deck[index];
		if ((faction.Affinity == "AffinityVaulters" || faction.Affinity == "AffinityMezari") && !faction.IsCustom)
		{
			if (this.VaulterDrawn)
			{
				this.deck.RemoveAt(index);
				index = this.deck.Count - 1;
				faction = this.deck[index];
			}
			else
			{
				this.VaulterDrawn = true;
			}
		}
		this.deck.RemoveAt(index);
		return faction;
	}

	internal override void AddRef(Faction faction)
	{
		this.deck.RemoveAll((Faction item) => item.Name == faction.Name);
		if ((faction.Affinity == "AffinityVaulters" || faction.Affinity == "AffinityMezari") && !faction.IsCustom)
		{
			this.VaulterDrawn = true;
		}
	}

	private List<Faction> deck = new List<Faction>();

	private bool VaulterDrawn;
}
