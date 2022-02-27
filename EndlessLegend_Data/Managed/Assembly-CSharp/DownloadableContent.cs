using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Interop;

public abstract class DownloadableContent
{
	public DownloadableContent(uint number, StaticString name, uint steamAppId)
	{
		if (StaticString.IsNullOrEmpty(name))
		{
			throw new ArgumentNullException("name");
		}
		this.Number = number;
		this.Name = name;
		this.ReleaseDate = new DateTime(2014, 9, 18, 18, 0, (int)number);
		this.SteamAppId = steamAppId;
		this.IsDynamicActivationEnabled = true;
		this.Type = DownloadableContentType.Undefined;
		this.Sharing = DownloadableContentSharing.None;
		this.Restrictions = new List<DownloadableContentRestriction>();
	}

	public DownloadableContent(uint number, StaticString name, uint steamAppId, DownloadableContentType type, bool dynamicActivationEnabled, DownloadableContentSharing sharing, bool introducesNewFaction = false, string introducedFactionName = "") : this(number, name, steamAppId)
	{
		this.Type = type;
		this.IsDynamicActivationEnabled = dynamicActivationEnabled;
		this.Sharing = sharing;
		this.IntroducesNewFaction = introducesNewFaction;
		this.IntroducedFactionName = introducedFactionName;
	}

	public DownloadableContentAccessibility Accessibility { get; set; }

	public abstract string Description { get; }

	public virtual bool IsInstalled
	{
		get
		{
			Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
			return steamApps != null && steamApps.BIsDlcInstalled(this.SteamAppId);
		}
	}

	public virtual bool IsSubscribed
	{
		get
		{
			Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
			return steamApps != null && steamApps.BIsSubscribedApp(this.SteamAppId);
		}
	}

	public DateTime ReleaseDate { get; protected set; }

	public List<DownloadableContentRestriction> Restrictions { get; private set; }

	public bool TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory category, string contentId, out bool result, out string replacement)
	{
		result = true;
		replacement = string.Empty;
		if (category == DownloadableContentRestrictionCategory.Undefined)
		{
			return false;
		}
		if (string.IsNullOrEmpty(contentId))
		{
			return false;
		}
		if (this.Restrictions.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < this.Restrictions.Count; i++)
		{
			if (this.Restrictions[i].Category == category)
			{
				bool flag = Amplitude.String.WildcardCompare(contentId, this.Restrictions[i].Wildcard, this.Restrictions[i].IgnoreCase);
				if (flag && (this.Accessibility & this.Restrictions[i].Accessibility) == DownloadableContentAccessibility.None)
				{
					replacement = this.Restrictions[i].Replacement;
					result = false;
					return true;
				}
			}
		}
		return true;
	}

	public bool TryCheckAgainstRestrictions(DownloadableContentRestrictionCategory category, string contentId, out bool result, out bool found, out string replacement)
	{
		result = true;
		replacement = string.Empty;
		found = false;
		if (category == DownloadableContentRestrictionCategory.Undefined)
		{
			return false;
		}
		if (string.IsNullOrEmpty(contentId))
		{
			return false;
		}
		if (this.Restrictions.Count == 0)
		{
			return true;
		}
		for (int i = 0; i < this.Restrictions.Count; i++)
		{
			if (this.Restrictions[i].Category == category)
			{
				bool flag = Amplitude.String.WildcardCompare(contentId, this.Restrictions[i].Wildcard, this.Restrictions[i].IgnoreCase);
				if (flag)
				{
					found = true;
					if ((this.Accessibility & this.Restrictions[i].Accessibility) == DownloadableContentAccessibility.None)
					{
						replacement = this.Restrictions[i].Replacement;
						result = false;
						return true;
					}
				}
			}
		}
		return true;
	}

	public const string SharedByClient = "sbc";

	public const string SharedByServer = "sbs";

	public readonly bool IsDynamicActivationEnabled;

	public readonly StaticString Name;

	public readonly uint Number;

	public readonly uint SteamAppId;

	public readonly bool IntroducesNewFaction;

	public readonly string IntroducedFactionName;

	public readonly DownloadableContentType Type;

	public readonly DownloadableContentSharing Sharing;

	public static class CollectorEdition
	{
		public static bool IsInstalled
		{
			get
			{
				Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
				return steamApps != null && steamApps.BIsSubscribedApp(4294967294u);
			}
		}

		internal const uint SteamAppId = 4294967294u;

		public static readonly StaticString ReadOnlyName = new StaticString("CollectorEdition");
	}

	public static class DungeonOfTheEndlessFounderPack
	{
		public static bool IsSubscribed
		{
			get
			{
				Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
				return steamApps != null && steamApps.BIsSubscribedApp(268080u);
			}
		}

		internal const uint SteamAppId = 268080u;

		public static readonly StaticString ReadOnlyName = new StaticString("DungeonOfTheEndlessFounderPack");

		public static readonly StaticString FactionName = new StaticString("FactionMezari");

		public static readonly StaticString FactionAffinityName = new StaticString("AffinityMezari");
	}

	public static class FounderPack
	{
		public static bool IsSubscribed
		{
			get
			{
				Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
				return steamApps != null && steamApps.BIsSubscribedApp(297650u);
			}
		}

		internal const uint SteamAppId = 297650u;

		public static readonly StaticString ReadOnlyName = new StaticString("FounderPack");
	}
}
