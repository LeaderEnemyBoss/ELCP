using System;
using System.Collections.Generic;
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
			if (global::BackgroundImageBootstrapper.ChosenIndex < 0)
			{
				bool value = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>("Settings/ELCP/UI/RandomizeLoadingScreen", false);
				global::BackgroundImageBootstrapper.PossibleTextures.Add(-1);
				if (this.downloadableContentAdvertisement != null && this.downloadableContentAdvertisement.Length != 0)
				{
					for (int i = this.downloadableContentAdvertisement.Length - 1; i >= 0; i--)
					{
						if (this.downloadableContentAdvertisement[i].SteamAppId != 0 && this.downloadableContentAdvertisement[i].BackgroundTexture != null)
						{
							Steamworks.SteamApps steamApps = Steamworks.SteamAPI.SteamApps;
							if (steamApps != null && steamApps.BIsSubscribedApp((uint)this.downloadableContentAdvertisement[i].SteamAppId))
							{
								if (!value)
								{
									global::BackgroundImageBootstrapper.PossibleTextures.Add(i);
									break;
								}
								global::BackgroundImageBootstrapper.PossibleTextures.Add(i);
							}
						}
					}
				}
				if (!value)
				{
					global::BackgroundImageBootstrapper.ChosenIndex = global::BackgroundImageBootstrapper.PossibleTextures.Count - 1;
				}
				else
				{
					global::BackgroundImageBootstrapper.ChosenIndex = UnityEngine.Random.Range(0, global::BackgroundImageBootstrapper.PossibleTextures.Count);
				}
			}
			if (global::BackgroundImageBootstrapper.ChosenIndex == 0)
			{
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
			if (!string.IsNullOrEmpty(text))
			{
				if (text == "schinese")
				{
					return this.downloadableContentAdvertisement[global::BackgroundImageBootstrapper.PossibleTextures[global::BackgroundImageBootstrapper.ChosenIndex]].SChineseSpecificBackgroundTexture;
				}
				if (text == "tchinese")
				{
					return this.downloadableContentAdvertisement[global::BackgroundImageBootstrapper.PossibleTextures[global::BackgroundImageBootstrapper.ChosenIndex]].TChineseSpecificBackgroundTexture;
				}
			}
			return this.downloadableContentAdvertisement[global::BackgroundImageBootstrapper.PossibleTextures[global::BackgroundImageBootstrapper.ChosenIndex]].BackgroundTexture;
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

	private static int ChosenIndex = -99;

	private static List<int> PossibleTextures = new List<int>();

	[Serializable]
	private struct DownloadableContentAdvertisement
	{
		public int SteamAppId;

		public Texture2D BackgroundTexture;

		public Texture2D SChineseSpecificBackgroundTexture;

		public Texture2D TChineseSpecificBackgroundTexture;
	}
}
