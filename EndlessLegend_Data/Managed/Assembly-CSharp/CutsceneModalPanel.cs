using System;
using System.Collections;
using System.IO;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Localization;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Video;
using BinkInterface;
using UnityEngine;

public class CutsceneModalPanel : GuiModalPanel
{
	private protected Action ActionVideoPlaybackComplete { protected get; private set; }

	private protected int CurrentSubtitleIndex { protected get; private set; }

	private protected int NextSubtitleIndex { protected get; private set; }

	protected bool DisplaySubtitles { get; set; }

	private protected VideoSubtitle[] Subtitles { protected get; private set; }

	public void OnDisableCB()
	{
	}

	public override bool HandleCancelRequest()
	{
		this.OnVideoPlaybackComplete();
		return true;
	}

	protected override void Awake()
	{
		base.Awake();
		if (this.OSXFallbackVideoFrame != null)
		{
			this.OSXFallbackVideoFrame.enabled = false;
			UnityEngine.Object.DestroyImmediate(this.OSXFallbackVideoFrame);
			this.OSXFallbackVideoFrame = null;
		}
		base.enabled = false;
		this.elapsedTime = -1.0;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.enabled = false;
		yield return base.OnHide(instant);
		if (this.VideoFrame != null)
		{
			this.VideoFrame.UnloadMovie();
		}
		if (this.OSXFallbackVideoFrame != null)
		{
			this.OSXFallbackVideoFrame.UnloadMovie();
		}
		if (this.ActionVideoPlaybackComplete != null)
		{
			this.ActionVideoPlaybackComplete();
			this.ActionVideoPlaybackComplete = null;
		}
		this.Subtitles = null;
		this.CurrentSubtitleIndex = 0;
		this.DisplaySubtitles = false;
		this.elapsedTime = -1.0;
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		base.enabled = true;
		this.ActionVideoPlaybackComplete = null;
		Amplitude.Unity.Video.IVideoSettingsService videoSettingsService = Services.GetService<Amplitude.Unity.Video.IVideoSettingsService>();
		this.DisplaySubtitles = videoSettingsService.DisplaySubtitles;
		this.CutsceneSubtitle.AgeTransform.Visible = false;
		if (this.SubtitleBackdrop != null)
		{
			this.SubtitleBackdrop.AgeTransform.Visible = false;
		}
		if (parameters == null)
		{
			throw new InvalidOperationException();
		}
		if (parameters.Length >= 2)
		{
			this.ActionVideoPlaybackComplete = (parameters[1] as Action);
			if (this.ActionVideoPlaybackComplete == null)
			{
				Diagnostics.LogWarning("Invalid parameters[1] that should be of type 'System.Action'.");
			}
		}
		if (parameters.Length >= 1)
		{
			string moviePath = parameters[0] as string;
			string moviesFolder = "Movies/";
			string moviePathFallback = null;
			string language = "english";
			ILocalizationService localizationService = Services.GetService<ILocalizationService>();
			if (localizationService != null && !string.IsNullOrEmpty(localizationService.CurrentLanguage))
			{
				language = localizationService.CurrentLanguage;
				if (language == "tchinese" || language == "schinese")
				{
					moviePathFallback = moviePath;
					string chineseMoviesFolder = "Chinese " + moviesFolder;
					moviePath = moviePath.Replace(moviesFolder, chineseMoviesFolder);
					moviesFolder = chineseMoviesFolder;
				}
			}
			if (!string.IsNullOrEmpty(moviePath))
			{
				if (moviePathFallback != null && !File.Exists(moviePath))
				{
					moviePath = moviePathFallback;
					language = "english";
				}
				if (File.Exists(moviePath) && this.VideoFrame != null)
				{
					float masterVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(Amplitude.Unity.Audio.AudioManager.Registers.MasterVolume, 1f);
					bool masterMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(Amplitude.Unity.Audio.AudioManager.Registers.MasterMute, false);
					this.VideoFrame.Volume = ((!masterMute) ? masterVolume : 0f);
					this.VideoFrame.LoadMovie(moviePath, false, false);
					if (this.DisplaySubtitles && this.VideoFrame.BinkMovie != null)
					{
						string extension = string.Format(".Subtitles-{0}.xml", language);
						string fileName = System.IO.Path.ChangeExtension(moviePath, extension);
						if (File.Exists(fileName))
						{
							ISerializationService serializationService = Services.GetService<ISerializationService>();
							if (serializationService != null)
							{
								XmlSerializer serializer = serializationService.GetXmlSerializer<VideoSubtitles>();
								if (serializer != null)
								{
									using (Stream stream = File.OpenRead(fileName))
									{
										VideoSubtitles videoSubtitles = serializer.Deserialize(stream) as VideoSubtitles;
										if (videoSubtitles != null && videoSubtitles.Subtitles != null)
										{
											this.Subtitles = videoSubtitles.Subtitles;
											this.CurrentSubtitleIndex = videoSubtitles.Subtitles.Length;
											this.NextSubtitleIndex = 0;
											this.binkSummary = default(Bink.BINKSUMMARY);
										}
									}
								}
							}
						}
					}
					else
					{
						this.Subtitles = null;
						this.CurrentSubtitleIndex = 0;
						this.NextSubtitleIndex = 0;
					}
				}
			}
		}
		yield break;
	}

	protected virtual void Update()
	{
		if (this.VideoFrame != null && this.VideoFrame.BinkMovie != null && !this.VideoFrame.BinkMoviePlaybackComplete)
		{
			this.elapsedTime += (double)Time.deltaTime;
			if (!Input.GetMouseButtonDown(0))
			{
				if (!Input.anyKeyDown)
				{
					if (this.DisplaySubtitles && this.Subtitles != null)
					{
						Bink.BinkGetSummary(this.VideoFrame.BinkMovie.BinkMovieData.Bink, ref this.binkSummary);
						if (this.binkSummary.FrameRate > 0u && this.binkSummary.FrameRateDiv > 0u)
						{
							double num = this.binkSummary.TotalPlayedFrames / (this.binkSummary.FrameRate / this.binkSummary.FrameRateDiv);
							if (this.CurrentSubtitleIndex < this.Subtitles.Length && this.Subtitles[this.CurrentSubtitleIndex].EndTime.TotalSeconds < num)
							{
								this.NextSubtitleIndex = this.CurrentSubtitleIndex + 1;
								this.CurrentSubtitleIndex = this.Subtitles.Length;
								this.CutsceneSubtitle.AgeTransform.Visible = false;
								if (this.SubtitleBackdrop != null)
								{
									this.SubtitleBackdrop.AgeTransform.Visible = false;
								}
							}
							if (this.NextSubtitleIndex < this.Subtitles.Length && this.Subtitles[this.NextSubtitleIndex].StartTime.TotalSeconds <= num)
							{
								this.CurrentSubtitleIndex = this.NextSubtitleIndex;
								string text = this.Subtitles[this.NextSubtitleIndex].Text;
								string key = this.Subtitles[this.NextSubtitleIndex].Key;
								if (!string.IsNullOrEmpty(key))
								{
									ILocalizationService service = Services.GetService<ILocalizationService>();
									if (service != null)
									{
										text = service.Localize(key, text);
									}
								}
								this.CutsceneSubtitle.Text = text;
								this.CutsceneSubtitle.AgeTransform.Visible = true;
								if (this.SubtitleBackdrop != null)
								{
									this.SubtitleBackdrop.AgeTransform.Visible = true;
								}
								this.NextSubtitleIndex = this.Subtitles.Length;
							}
						}
					}
					return;
				}
			}
		}
		this.OnVideoPlaybackComplete();
	}

	private void OnVideoPlaybackComplete()
	{
		this.Hide(false);
	}

	public AgePrimitiveImage SubtitleBackdrop;

	public AgePrimitiveLabel CutsceneSubtitle;

	public AgePrimitiveBinkMovie VideoFrame;

	public AgePrimitiveMovie OSXFallbackVideoFrame;

	private Bink.BINKSUMMARY binkSummary;

	private double elapsedTime = -1.0;
}
