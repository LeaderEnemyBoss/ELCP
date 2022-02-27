using System;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using UnityEngine;

public class BackgroundImageBootstrapper : Amplitude.Unity.Framework.BackgroundImageBootstrapper
{
	protected override Texture BackgroundTexture
	{
		get
		{
			if (this.endlessDayBackgroundTexture != null && DownloadableContent8.EndlessDay.IsActive)
			{
				return this.endlessDayBackgroundTexture;
			}
			string text;
			global::Application.ResolveChineseLanguage(out text);
			if (this.downloadableContentAdvertisement != null && this.downloadableContentAdvertisement.Length > 0)
			{
				for (int i = this.downloadableContentAdvertisement.Length - 1; i >= 0; i--)
				{
					if (this.downloadableContentAdvertisement[i].SteamAppId != 0 && this.downloadableContentAdvertisement[i].BackgroundTexture != null)
					{
						Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
						if (steamApps != null)
						{
							bool flag = steamApps.BIsSubscribedApp((uint)this.downloadableContentAdvertisement[i].SteamAppId);
							if (flag)
							{
								if (!string.IsNullOrEmpty(text))
								{
									if (text == "schinese")
									{
										return this.downloadableContentAdvertisement[i].SChineseSpecificBackgroundTexture;
									}
									if (text == "tchinese")
									{
										return this.downloadableContentAdvertisement[i].TChineseSpecificBackgroundTexture;
									}
								}
								return this.downloadableContentAdvertisement[i].BackgroundTexture;
							}
						}
					}
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				if (text == "schinese")
				{
					return this.schineseBackgroundTexture;
				}
				if (text == "tchinese")
				{
					return this.tchineseBackgroundTexture;
				}
			}
			return base.BackgroundTexture;
		}
	}

	[SerializeField]
	private Texture2D endlessDayBackgroundTexture;

	[SerializeField]
	private Texture2D schineseBackgroundTexture;

	[SerializeField]
	private Texture2D tchineseBackgroundTexture;

	[SerializeField]
	private global::BackgroundImageBootstrapper.DownloadableContentAdvertisement[] downloadableContentAdvertisement;

	[Serializable]
	private struct DownloadableContentAdvertisement
	{
		public int SteamAppId;

		public Texture2D BackgroundTexture;

		public Texture2D SChineseSpecificBackgroundTexture;

		public Texture2D TChineseSpecificBackgroundTexture;
	}
}
