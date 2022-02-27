using System;
using System.Collections.Generic;
using System.Linq;
using Amplitude.Unity.Framework;

public abstract class FactionRandomizer
{
	public FactionRandomizer(Session session)
	{
		List<Faction> list = (from faction in Databases.GetDatabase<Faction>(true)
		where faction.IsStandard && !faction.IsHidden && !faction.HasToBeRandomized
		select faction).ToList<Faction>();
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null)
		{
			bool result;
			list.RemoveAll((Faction faction) => downloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.Faction, faction.Name, out result) && !result);
			list.RemoveAll((Faction faction) => faction.Affinity != null && downloadableContentService.TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory.FactionAffinity, faction.Affinity, out result) && !result);
		}
		if (session != null)
		{
			for (int i = list.Count - 1; i >= 0; i--)
			{
				Faction faction2 = list[i];
				if (Faction.IsOptionDefinitionConstrained(faction2, session) || faction2.Name == "FactionELCPSpectator")
				{
					list.RemoveAt(i);
				}
			}
		}
		this.factions = list.ToArray();
	}

	public abstract Faction Next();

	internal virtual void AddRef(Faction faction)
	{
	}

	protected Faction[] factions;
}
