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
		Amplitude.Unity.Video.IVideoSettingsService service = Services.GetService<Amplitude.Unity.Video.IVideoSettingsService>();
		this.DisplaySubtitles = service.DisplaySubtitles;
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
			string text = parameters[0] as string;
			string text2 = "Movies/";
			string text3 = null;
			string text4 = "english";
			ILocalizationService service2 = Services.GetService<ILocalizationService>();
			if (service2 != null && !string.IsNullOrEmpty(service2.CurrentLanguage))
			{
				text4 = service2.CurrentLanguage;
				if (text4 == "tchinese" || text4 == "schinese")
				{
					text3 = text;
					string newValue = "Chinese " + text2;
					text = text.Replace(text2, newValue);
				}
			}
			if (!string.IsNullOrEmpty(text))
			{
				if (text3 != null && !File.Exists(text))
				{
					text = text3;
					text4 = "english";
				}
				if (File.Exists(text) && this.VideoFrame != null)
				{
					float value = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(Amplitude.Unity.Audio.AudioManager.Registers.MasterVolume, 1f);
					bool value2 = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(Amplitude.Unity.Audio.AudioManager.Registers.MasterMute, false);
					this.VideoFrame.Volume = ((!value2) ? value : 0f);
					this.VideoFrame.LoadMovie(text, false, false);
					if (this.DisplaySubtitles && this.VideoFrame.BinkMovie != null)
					{
						string extension = string.Format(".Subtitles-{0}.xml", text4);
						string path = System.IO.Path.ChangeExtension(text, extension);
						if (!File.Exists(path))
						{
							goto IL_2BD;
						}
						ISerializationService service3 = Services.GetService<ISerializationService>();
						if (service3 == null)
						{
							goto IL_2BD;
						}
						XmlSerializer xmlSerializer = service3.GetXmlSerializer<VideoSubtitles>();
						if (xmlSerializer == null)
						{
							goto IL_2BD;
						}
						using (Stream stream = File.OpenRead(path))
						{
							VideoSubtitles videoSubtitles = xmlSerializer.Deserialize(stream) as VideoSubtitles;
							if (videoSubtitles != null && videoSubtitles.Subtitles != null)
							{
								this.Subtitles = videoSubtitles.Subtitles;
								this.CurrentSubtitleIndex = videoSubtitles.Subtitles.Length;
								this.NextSubtitleIndex = 0;
								this.binkSummary = default(Bink.BINKSUMMARY);
							}
							yield break;
						}
					}
					this.Subtitles = null;
					this.CurrentSubtitleIndex = 0;
					this.NextSubtitleIndex = 0;
				}
			}
		}
		IL_2BD:
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
