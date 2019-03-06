using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Amplitude.Extensions;
using Amplitude.Unity.Framework;
using FMOD;
using UnityEngine;

namespace Amplitude.Unity.Audio
{
	[Diagnostics.TagAttribute("Audio")]
	[Diagnostics.TagAttribute("Audio")]
	public class AudioManager : Manager, IAudioEventService, IAudioLayeredMusicService, IAudioMusicService, IAudioService, IAudioSettingsService, IService
	{
		public bool IsEventAuthorized(StaticString eventName)
		{
			return !StaticString.IsNullOrEmpty(eventName);
		}

		public void RegisterEventInstance(StaticString eventName, FMOD.Event instance)
		{
		}

		public void ReportError(StaticString eventName)
		{
		}

		public FMOD.Event Get2DEvent(StaticString eventName, EVENT_MODE mode)
		{
			if (!this.fmodSystemInitialized)
			{
				return null;
			}
			if (eventName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Event name is empty.");
				return null;
			}
			if (!this.IsEventAuthorized(eventName))
			{
				return null;
			}
			FMOD.Event @event = null;
			string fmodPath = this.GetFmodPath(eventName);
			this.ErrorCheck(this.fmodEventSystem.getEvent(fmodPath, mode, ref @event));
			this.ReportError(eventName);
			if (base.LastError == 0)
			{
				this.RegisterEventInstance(eventName, @event);
				return @event;
			}
			return null;
		}

		public FMOD.Event Get3DEvent(StaticString eventName, EVENT_MODE mode, VECTOR position, VECTOR velocity, VECTOR orientation)
		{
			if (!this.fmodSystemInitialized)
			{
				return null;
			}
			if (eventName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Event name is empty.");
				return null;
			}
			if (!this.IsEventAuthorized(eventName))
			{
				return null;
			}
			FMOD.Event @event = null;
			string fmodPath = this.GetFmodPath(eventName);
			Diagnostics.Assert(this.fmodEventSystem != null);
			this.ErrorCheck(this.fmodEventSystem.getEvent(fmodPath, EVENT_MODE.INFOONLY, ref @event));
			this.ReportError(eventName);
			if (@event == null)
			{
				return null;
			}
			this.ErrorCheck(@event.set3DAttributes(ref position, ref velocity, ref orientation));
			Diagnostics.Assert(this.fmodEventSystem != null);
			this.ErrorCheck(this.fmodEventSystem.getEvent(fmodPath, mode, ref @event));
			if (base.LastError == 0)
			{
				this.RegisterEventInstance(eventName, @event);
				return @event;
			}
			return null;
		}

		public FMOD.Event Get3DEvent(StaticString eventName, EVENT_MODE mode, Vector3 position, Vector3 velocity, Vector3 orientation)
		{
			if (eventName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Event name is empty.");
				return null;
			}
			VECTOR position2 = position.ToFmodVector();
			VECTOR velocity2 = velocity.ToFmodVector();
			VECTOR orientation2 = orientation.ToFmodVector();
			return this.Get3DEvent(eventName, EVENT_MODE.DEFAULT, position2, velocity2, orientation2);
		}

		public bool IsEventPlaying(FMOD.Event fmodEvent)
		{
			if (!this.fmodSystemInitialized)
			{
				return false;
			}
			if (fmodEvent == null)
			{
				return false;
			}
			EVENT_STATE event_STATE = EVENT_STATE.ERROR;
			RESULT state = fmodEvent.getState(ref event_STATE);
			return state == RESULT.OK && (event_STATE & EVENT_STATE.PLAYING) != (EVENT_STATE)0;
		}

		public void PlayEvent(FMOD.Event fmodEvent)
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			if (fmodEvent == null)
			{
				return;
			}
			this.ErrorCheck(fmodEvent.start());
		}

		public FMOD.Event Play2DEvent(StaticString eventName)
		{
			if (!this.fmodSystemInitialized)
			{
				return null;
			}
			if (eventName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Event name is empty.");
				return null;
			}
			FMOD.Event @event = this.Get2DEvent(eventName, EVENT_MODE.DEFAULT);
			if (@event != null && !this.ErrorCheck(@event.start()))
			{
				return @event;
			}
			return null;
		}

		public FMOD.Event Play3DEvent(StaticString eventName, Vector3 position, Vector3 velocity, Vector3 orientation)
		{
			if (!this.fmodSystemInitialized)
			{
				return null;
			}
			if (eventName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Event name is empty.");
				return null;
			}
			FMOD.Event @event = this.Get3DEvent(eventName, EVENT_MODE.DEFAULT, position, velocity, orientation);
			if (@event != null && !this.ErrorCheck(@event.start()))
			{
				return @event;
			}
			return null;
		}

		public void StopEvent(FMOD.Event fmodEvent, bool immediate = true, bool silent = false)
		{
			if (fmodEvent == null)
			{
				return;
			}
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			EVENT_STATE event_STATE = EVENT_STATE.ERROR;
			fmodEvent.getState(ref event_STATE);
			if ((event_STATE & EVENT_STATE.PLAYING) == EVENT_STATE.PLAYING)
			{
				this.ErrorCheck(fmodEvent.stop(immediate));
			}
		}

		private IEnumerator BindAudioEventService()
		{
			if (!this.fmodSystemInitialized)
			{
				Services.AddService<IAudioEventService>(this.nullAudioManager);
				yield break;
			}
			Services.AddService<IAudioEventService>(this);
			yield return null;
			yield break;
		}

		private string GetFmodPath(StaticString fmodPath)
		{
			if (fmodPath == StaticString.Empty)
			{
				Diagnostics.LogWarning("Fmod path is empty.");
				return null;
			}
			return this.FmodProjectName + '/' + fmodPath;
		}

		protected ReadOnlyCollection<LayeredEvent> LayeredEvents
		{
			get
			{
				if (this.readOnlyLayeredEvents == null)
				{
					Diagnostics.Assert(this.currentLayeredEvents != null);
					this.readOnlyLayeredEvents = this.currentLayeredEvents.AsReadOnly();
				}
				return this.readOnlyLayeredEvents;
			}
		}

		protected IDatabase<LayeredEventDefinition> LayeredEventDatabase
		{
			get
			{
				if (this.layeredEventDatabase == null)
				{
					this.layeredEventDatabase = Databases.GetDatabase<LayeredEventDefinition>(false);
				}
				return this.layeredEventDatabase;
			}
		}

		protected LayeredEvent PlayLayeredEvent(LayeredEventDefinition layeredEventDefinition, StaticString layeredEventName)
		{
			if (layeredEventDefinition == null)
			{
				throw new ArgumentNullException("layeredEventDefinition");
			}
			if (StaticString.IsNullOrEmpty(layeredEventName))
			{
				throw new ArgumentNullException("layeredEventName");
			}
			if (this.currentLayeredEventByNames.ContainsKey(layeredEventName))
			{
				Diagnostics.LogError("Layered event {0} is already played.");
				return null;
			}
			LayeredEvent layeredEvent = new LayeredEvent(layeredEventDefinition, layeredEventName, this.MusicIntervalDurationRatio);
			Diagnostics.Assert(this.currentLayeredEventByNames != null);
			this.currentLayeredEventByNames.Add(layeredEventName, layeredEvent);
			Diagnostics.Assert(this.currentLayeredEvents != null);
			this.currentLayeredEvents.Add(layeredEvent);
			return layeredEvent;
		}

		protected void SetLayerValue(StaticString layeredEventName, StaticString layerName, float value)
		{
			if (StaticString.IsNullOrEmpty(layeredEventName))
			{
				throw new ArgumentNullException("layeredEventName");
			}
			Diagnostics.Assert(this.currentLayeredEventByNames != null);
			if (!this.currentLayeredEventByNames.ContainsKey(layeredEventName))
			{
				return;
			}
			Diagnostics.Assert(this.currentLayeredEventByNames[layeredEventName] != null);
			this.currentLayeredEventByNames[layeredEventName].SetLayerValue(layerName, value);
		}

		protected void StopLayeredEvent(StaticString layeredEventName, bool immediate = true)
		{
			if (StaticString.IsNullOrEmpty(layeredEventName))
			{
				throw new ArgumentNullException("layeredEventName");
			}
			Diagnostics.Assert(this.currentLayeredEventByNames != null);
			if (!this.currentLayeredEventByNames.ContainsKey(layeredEventName))
			{
				return;
			}
			LayeredEvent layeredEvent = this.currentLayeredEventByNames[layeredEventName];
			if (this.fmodSystemInitialized)
			{
				Diagnostics.Assert(layeredEvent != null);
				layeredEvent.Stop(immediate);
			}
			Diagnostics.Assert(this.currentLayeredEvents != null);
			this.currentLayeredEvents.Remove(layeredEvent);
			this.currentLayeredEventByNames.Remove(layeredEventName);
		}

		private void UpdateEventLayerSystem(float currentTime, float deltaTime)
		{
			Diagnostics.Assert(this.currentLayeredEvents != null);
			for (int i = this.currentLayeredEvents.Count - 1; i >= 0; i--)
			{
				Diagnostics.Assert(this.currentLayeredEvents != null);
				LayeredEvent layeredEvent = this.currentLayeredEvents[i];
				Diagnostics.Assert(layeredEvent != null);
				if (!layeredEvent.Update(this, currentTime, deltaTime))
				{
					this.StopLayeredEvent(layeredEvent.Name, false);
					this.UnregisterLayeredMusic(layeredEvent.Name);
				}
			}
			this.UpdateLayeredMusic();
		}

		public StaticString CurrentLayeredMusicName
		{
			get
			{
				if (this.CurrentLayeredMusic == null)
				{
					return StaticString.Empty;
				}
				return this.CurrentLayeredMusic.Name;
			}
		}

		private AudioManager.Music CurrentLayeredMusic
		{
			get
			{
				Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
				if (this.currentLayeredMusicDefinitions.Count == 0)
				{
					return null;
				}
				if (this.currentLayeredMusicDefinitions[0].Priority == 2147483647)
				{
					return null;
				}
				return this.currentLayeredMusicDefinitions[0];
			}
		}

		public void InsertEventInLayeredMusic(StaticString musicName, StaticString eventName, int index = -1, float silenceIntervalDuration = float.NaN)
		{
			if (StaticString.IsNullOrEmpty(eventName))
			{
				throw new ArgumentNullException("eventName");
			}
			if (StaticString.IsNullOrEmpty(musicName))
			{
				throw new ArgumentNullException("musicName");
			}
			if (this.LayeredEventDatabase == null)
			{
				return;
			}
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			AudioManager.Music music = this.currentLayeredMusicDefinitions.Find((AudioManager.Music match) => match.Name == musicName);
			if (music == null)
			{
				Diagnostics.LogWarning("Can't insert event {0} in music {1}, because this music doesn't exist.", new object[]
				{
					eventName,
					musicName
				});
				return;
			}
			Diagnostics.Assert(music.LayeredEvent != null);
			music.LayeredEvent.InsertTheme(index, eventName, silenceIntervalDuration);
		}

		public bool IsMusicPlaying(StaticString musicName)
		{
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			return this.currentLayeredMusicDefinitions.Any((AudioManager.Music match) => match.Name == musicName);
		}

		public void NextLayeredMusicTrack(StaticString musicName, bool immediate)
		{
			if (StaticString.IsNullOrEmpty(musicName))
			{
				throw new ArgumentNullException("musicName");
			}
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			AudioManager.Music music = this.currentLayeredMusicDefinitions.Find((AudioManager.Music match) => match.Name == musicName);
			if (music == null)
			{
				Diagnostics.LogWarning("Can't play the next track of the music {1}, because this music doesn't exist.", new object[]
				{
					musicName
				});
				return;
			}
			Diagnostics.Assert(music.LayeredEvent != null);
			music.LayeredEvent.Next(immediate);
		}

		public LayeredEvent PlayLayeredMusic(StaticString layeredMusicDefinitionReference, StaticString musicName, int priority)
		{
			if (StaticString.IsNullOrEmpty(layeredMusicDefinitionReference))
			{
				throw new ArgumentNullException("layeredMusicDefinitionReference");
			}
			if (StaticString.IsNullOrEmpty(musicName))
			{
				throw new ArgumentNullException("musicName");
			}
			if (this.LayeredEventDatabase == null)
			{
				return null;
			}
			if (this.CurrentLayeredMusic != null && this.CurrentLayeredMusic.Name == musicName)
			{
				return null;
			}
			LayeredEventDefinition layeredEventDefinition;
			if (!this.LayeredEventDatabase.TryGetValue(layeredMusicDefinitionReference, out layeredEventDefinition))
			{
				Diagnostics.LogWarning("Layered music <{0}> not found in layered event datatable.", new object[]
				{
					layeredMusicDefinitionReference
				});
				return null;
			}
			LayeredMusicDefinition layeredMusicDefinition = layeredEventDefinition as LayeredMusicDefinition;
			if (layeredMusicDefinition == null)
			{
				Diagnostics.LogWarning("Layered event <{0}> is not a layered music definition.", new object[]
				{
					layeredMusicDefinitionReference
				});
				return null;
			}
			LayeredEvent layeredEvent = this.PlayLayeredEvent(layeredMusicDefinition, musicName);
			if (layeredEvent == null)
			{
				Diagnostics.LogWarning("Can't play layered event <{0}>.", new object[]
				{
					layeredMusicDefinitionReference
				});
				return null;
			}
			this.PlayLayeredMusic(layeredEvent, priority);
			return layeredEvent;
		}

		public void StopAllMusics()
		{
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			foreach (AudioManager.Music music in this.currentLayeredMusicDefinitions)
			{
				this.StopLayeredEvent(music.Name, false);
			}
			this.currentLayeredMusicDefinitions.Clear();
		}

		public void StopMusic(StaticString musicName)
		{
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			int num = this.currentLayeredMusicDefinitions.FindIndex((AudioManager.Music match) => match.Name == musicName);
			if (num < 0)
			{
				Diagnostics.Log("Can't stop music (name: '{0}') because it is not currently running.", new object[]
				{
					musicName
				});
				return;
			}
			this.StopLayeredEvent(musicName, false);
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			this.currentLayeredMusicDefinitions.RemoveAt(num);
			this.UpdateMusicsVolume();
		}

		protected LayeredEvent GetLayeredMusic(StaticString musicName)
		{
			if (StaticString.IsNullOrEmpty(musicName))
			{
				throw new ArgumentNullException("musicName");
			}
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			AudioManager.Music music = this.currentLayeredMusicDefinitions.Find((AudioManager.Music match) => match.Name == musicName);
			if (music == null)
			{
				return null;
			}
			return music.LayeredEvent;
		}

		protected LayeredEvent PlayLayeredMusic(LayeredMusicDefinition layeredMusicDefinition, StaticString musicName, int priority)
		{
			if (layeredMusicDefinition == null)
			{
				throw new ArgumentNullException("layeredMusicDefinition");
			}
			if (StaticString.IsNullOrEmpty(musicName))
			{
				throw new ArgumentNullException("musicName");
			}
			if (this.CurrentLayeredMusic != null && this.CurrentLayeredMusic.Name == musicName)
			{
				return null;
			}
			LayeredEvent layeredEvent = this.PlayLayeredEvent(layeredMusicDefinition, musicName);
			this.PlayLayeredMusic(layeredEvent, priority);
			return layeredEvent;
		}

		protected void PlayLayeredMusic(LayeredEvent layeredMusic, int priority)
		{
			if (layeredMusic == null)
			{
				throw new ArgumentNullException("layeredMusic");
			}
			if (this.CurrentLayeredMusic != null && this.CurrentLayeredMusic.Name == layeredMusic.Name)
			{
				return;
			}
			Diagnostics.Log("Playing layered music <" + layeredMusic.Name + ">.");
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			AudioManager.Music music = this.currentLayeredMusicDefinitions.Find((AudioManager.Music match) => match.Name == layeredMusic.Name);
			if (music != null)
			{
				music.Priority = priority;
			}
			else
			{
				this.currentLayeredMusicDefinitions.Add(new AudioManager.Music(layeredMusic, priority));
			}
			this.currentLayeredMusicDefinitions.Sort((AudioManager.Music left, AudioManager.Music right) => left.Priority.CompareTo(right.Priority));
			this.UpdateMusicsVolume();
		}

		protected void SetLayeredMusicPriority(LayeredEvent layeredMusic, int priority)
		{
			if (layeredMusic == null)
			{
				throw new ArgumentNullException("layeredMusic");
			}
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			AudioManager.Music music = this.currentLayeredMusicDefinitions.Find((AudioManager.Music match) => match.Name == layeredMusic.Name);
			if (music == null)
			{
				return;
			}
			music.Priority = priority;
			this.currentLayeredMusicDefinitions.Sort((AudioManager.Music left, AudioManager.Music right) => left.Priority.CompareTo(right.Priority));
			this.UpdateMusicsVolume();
		}

		private IEnumerator BindLayeredMusicServices()
		{
			if (!this.fmodSystemInitialized)
			{
				Services.AddService<IAudioLayeredMusicService>(this.nullAudioManager);
				yield break;
			}
			Services.AddService<IAudioLayeredMusicService>(this);
			yield return null;
			yield break;
		}

		private void UnregisterLayeredMusic(StaticString musicName)
		{
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			int num = this.currentLayeredMusicDefinitions.FindIndex((AudioManager.Music match) => match.Name == musicName);
			if (num < 0)
			{
				return;
			}
			this.currentLayeredMusicDefinitions.RemoveAt(num);
			this.UpdateMusicsVolume();
		}

		private void UpdateLayeredMusic()
		{
			AudioManager.Music currentLayeredMusic = this.CurrentLayeredMusic;
			if (currentLayeredMusic != null)
			{
				Diagnostics.Assert(currentLayeredMusic.LayeredEvent != null);
				if (!currentLayeredMusic.LayeredEvent.AllowEventsToBePlayedTwice && currentLayeredMusic != this.lastPlayedMusic && this.lastPlayedMusic != null && this.lastPlayedMusic.LayeredEvent != null && currentLayeredMusic.LayeredEvent.FmodEventCurrentThemeName == this.lastPlayedMusic.LayeredEvent.FmodEventCurrentThemeName)
				{
					currentLayeredMusic.LayeredEvent.Next(false);
				}
				this.lastPlayedMusic = currentLayeredMusic;
			}
		}

		private void UpdateMusicsVolume()
		{
			Diagnostics.Assert(this.currentLayeredMusicDefinitions != null);
			if (this.currentLayeredMusicDefinitions.Count == 0)
			{
				return;
			}
			AudioManager.Music currentLayeredMusic = this.CurrentLayeredMusic;
			if (currentLayeredMusic != null)
			{
				Diagnostics.Assert(currentLayeredMusic.LayeredEvent != null);
				currentLayeredMusic.LayeredEvent.SetLayerValue(this.eventLayerVolumeName, 1f);
			}
			if (this.currentLayeredMusicDefinitions.Count > 1)
			{
				for (int i = 1; i < this.currentLayeredMusicDefinitions.Count; i++)
				{
					AudioManager.Music music = this.currentLayeredMusicDefinitions[i];
					music.LayeredEvent.SetLayerValue(this.eventLayerVolumeName, 0f);
				}
			}
		}

		public float GetParameterValue(uint audioParameterID)
		{
			Diagnostics.Assert(this.fmodMusicSystem != null, "Can't get music parameter value because Fmod music system is NULL.");
			float result = 0f;
			this.ErrorCheck(this.fmodMusicSystem.getParameterValue(audioParameterID, ref result));
			return result;
		}

		public void Play(uint audioCueID)
		{
			Diagnostics.Assert(this.fmodMusicSystem != null, "Can't play fmod music because fmod music system is NULL");
			if (!this.fmodMusicPrompts.ContainsKey(audioCueID))
			{
				MusicPrompt value = null;
				this.ErrorCheck(this.fmodMusicSystem.prepareCue(audioCueID, ref value));
				this.fmodMusicPrompts.Add(audioCueID, value);
			}
			if (this.fmodMusicPrompts[audioCueID] == null)
			{
				Diagnostics.LogWarning("Can't play music, fmod failed to prepare cue <" + audioCueID + ">");
				return;
			}
			this.ErrorCheck(this.fmodMusicPrompts[audioCueID].begin());
		}

		public void SetParameterValue(uint audioParameterID, float value)
		{
			Diagnostics.Assert(this.fmodMusicSystem != null, "Can't set parameter value because fmod music system is NULL");
			this.ErrorCheck(this.fmodMusicSystem.setParameterValue(audioParameterID, value));
		}

		public void Stop(uint audioCueID)
		{
			Diagnostics.Assert(this.fmodMusicSystem != null, "Can't stop fmod music because fmod music system is NULL");
			if (!this.fmodMusicPrompts.ContainsKey(audioCueID) || this.fmodMusicPrompts[audioCueID] == null)
			{
				Diagnostics.LogWarning("Can't stop this audio cue <" + audioCueID + "> because the music is not started.");
				return;
			}
			this.ErrorCheck(this.fmodMusicPrompts[audioCueID].end());
		}

		private IEnumerator BindMusicServices()
		{
			if (!this.fmodSystemInitialized)
			{
				Services.AddService<IAudioMusicService>(this.nullAudioManager);
				yield break;
			}
			Services.AddService<IAudioMusicService>(this);
			yield return null;
			yield break;
		}

		public bool AmbianceMute
		{
			get
			{
				if (this.fmodEventAmbianceCategory != null)
				{
					this.ErrorCheck(this.fmodEventAmbianceCategory.getMute(ref this.ambianceMute));
				}
				return this.ambianceMute;
			}
			set
			{
				this.ambianceMute = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.AmbianceMute, this.ambianceMute.ToString());
				if (this.fmodEventAmbianceCategory != null)
				{
					this.ErrorCheck(this.fmodEventAmbianceCategory.setMute(this.ambianceMute));
				}
			}
		}

		public float AmbianceVolume
		{
			get
			{
				if (this.fmodEventAmbianceCategory != null)
				{
					this.ErrorCheck(this.fmodEventAmbianceCategory.getVolume(ref this.ambianceVolume));
				}
				return this.ambianceVolume;
			}
			set
			{
				this.ambianceVolume = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.AmbianceVolume, this.ambianceVolume.ToString());
				if (this.fmodEventAmbianceCategory != null)
				{
					this.ErrorCheck(this.fmodEventAmbianceCategory.setVolume(this.ambianceVolume));
				}
			}
		}

		public bool GUIMute
		{
			get
			{
				if (this.fmodEventGUICategory != null)
				{
					this.ErrorCheck(this.fmodEventGUICategory.getMute(ref this.guiMute));
				}
				return this.guiMute;
			}
			set
			{
				this.guiMute = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.GUIMute, this.guiMute.ToString());
				if (this.fmodEventGUICategory != null)
				{
					this.ErrorCheck(this.fmodEventGUICategory.setMute(this.guiMute));
				}
			}
		}

		public float GUIVolume
		{
			get
			{
				if (this.fmodEventGUICategory != null)
				{
					this.ErrorCheck(this.fmodEventGUICategory.getVolume(ref this.guiVolume));
				}
				return this.guiVolume;
			}
			set
			{
				this.guiVolume = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.GUIVolume, this.guiVolume.ToString());
				if (this.fmodEventGUICategory != null)
				{
					this.ErrorCheck(this.fmodEventGUICategory.setVolume(this.guiVolume));
				}
			}
		}

		public bool IsAudioSettingsServiceAvailable
		{
			get
			{
				return true;
			}
		}

		public bool MasterMute
		{
			get
			{
				if (this.fmodEventMasterCategory != null)
				{
					this.ErrorCheck(this.fmodEventMasterCategory.getMute(ref this.masterMute));
				}
				return this.masterMute;
			}
			set
			{
				this.masterMute = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MasterMute, this.masterMute.ToString());
				this.ApplyMasterMute();
			}
		}

		public float MasterVolume
		{
			get
			{
				if (this.fmodEventMasterCategory != null)
				{
					this.ErrorCheck(this.fmodEventMasterCategory.getVolume(ref this.masterVolume));
				}
				return this.masterVolume;
			}
			set
			{
				this.masterVolume = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MasterVolume, this.masterVolume.ToString());
				if (this.fmodEventMasterCategory != null)
				{
					this.ErrorCheck(this.fmodEventMasterCategory.setVolume(this.masterVolume));
				}
			}
		}

		public bool MusicMute
		{
			get
			{
				if (this.fmodEventMusicCategory != null)
				{
					this.ErrorCheck(this.fmodEventMusicCategory.getMute(ref this.musicMute));
				}
				return this.musicMute;
			}
			set
			{
				this.musicMute = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MusicMute, this.musicMute.ToString());
				if (this.fmodEventMusicCategory != null)
				{
					this.ErrorCheck(this.fmodEventMusicCategory.setMute(this.musicMute));
				}
			}
		}

		public float MusicVolume
		{
			get
			{
				if (this.fmodEventMusicCategory != null)
				{
					this.ErrorCheck(this.fmodEventMusicCategory.getVolume(ref this.musicVolume));
				}
				return this.musicVolume;
			}
			set
			{
				this.musicVolume = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MusicVolume, this.musicVolume.ToString());
				if (this.fmodEventMusicCategory != null)
				{
					this.ErrorCheck(this.fmodEventMusicCategory.setVolume(this.musicVolume));
				}
			}
		}

		public float MusicIntervalDurationRatio
		{
			get
			{
				return this.musicIntervalDurationRatio;
			}
			set
			{
				this.musicIntervalDurationRatio = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MusicIntervalDurationRatio, this.musicIntervalDurationRatio.ToString());
				for (int i = 0; i < this.LayeredEvents.Count; i++)
				{
					LayeredEvent layeredEvent = this.LayeredEvents[i];
					layeredEvent.IntervalDurationRatio = this.musicIntervalDurationRatio;
				}
			}
		}

		public bool MuteWhenLostFocus
		{
			get
			{
				return this.muteWhenLostFocus;
			}
			set
			{
				if (this.muteWhenLostFocus != value)
				{
					this.muteWhenLostFocus = value;
					Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.MuteWhenLostFocus, this.muteWhenLostFocus.ToString());
					this.ApplyMasterMute();
				}
			}
		}

		public bool SFXMute
		{
			get
			{
				if (this.fmodEventSFXCategory != null)
				{
					this.ErrorCheck(this.fmodEventSFXCategory.getMute(ref this.sfxMute));
				}
				return this.sfxMute;
			}
			set
			{
				this.sfxMute = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.SFXMute, this.sfxMute.ToString());
				if (this.fmodEventSFXCategory != null)
				{
					this.ErrorCheck(this.fmodEventSFXCategory.setMute(this.sfxMute));
				}
			}
		}

		public float SFXVolume
		{
			get
			{
				if (this.fmodEventSFXCategory != null)
				{
					this.ErrorCheck(this.fmodEventSFXCategory.getVolume(ref this.sfxVolume));
				}
				return this.sfxVolume;
			}
			set
			{
				this.sfxVolume = value;
				Amplitude.Unity.Framework.Application.Registry.SetValue(AudioManager.Registers.SFXVolume, this.sfxVolume.ToString());
				if (this.fmodEventSFXCategory != null)
				{
					this.ErrorCheck(this.fmodEventSFXCategory.setVolume(this.sfxVolume));
				}
			}
		}

		private void ApplyMasterMute()
		{
			bool mute = this.masterMute | (this.muteWhenLostFocus & this.masterMuteWhenLostFocus);
			if (this.fmodEventMasterCategory != null)
			{
				this.ErrorCheck(this.fmodEventMasterCategory.setMute(mute));
			}
		}

		private IEnumerator BindAudioSettingsService()
		{
			if (!this.fmodSystemInitialized)
			{
				Services.AddService<IAudioSettingsService>(this.nullAudioManager);
				yield break;
			}
			this.fmodEventMusicCategory = this.GetCategoryByFmodName(this.musicCategoryFmodName);
			Diagnostics.Assert(this.fmodEventMusicCategory != null);
			this.fmodEventAmbianceCategory = this.GetCategoryByFmodName(this.ambianceCategoryFmodName);
			Diagnostics.Assert(this.fmodEventAmbianceCategory != null);
			this.fmodEventSFXCategory = this.GetCategoryByFmodName(this.sfxCategoryFmodName);
			Diagnostics.Assert(this.fmodEventSFXCategory != null);
			this.fmodEventGUICategory = this.GetCategoryByFmodName(this.guiCategoryFmodName);
			Diagnostics.Assert(this.fmodEventGUICategory != null);
			this.MuteWhenLostFocus = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.MuteWhenLostFocus, false);
			this.AmbianceVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.AmbianceVolume, 1f);
			this.AmbianceMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.AmbianceMute, false);
			this.GUIVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.GUIVolume, 1f);
			this.GUIMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.GUIMute, false);
			this.MasterVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.MasterVolume, 1f);
			this.MasterMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.MasterMute, false);
			this.MusicVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.MusicVolume, 1f);
			this.MusicMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.MusicMute, false);
			this.MusicIntervalDurationRatio = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.MusicIntervalDurationRatio, 0.5f);
			this.SFXVolume = Amplitude.Unity.Framework.Application.Registry.GetValue<float>(AudioManager.Registers.SFXVolume, 1f);
			this.SFXMute = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.SFXMute, false);
			Services.AddService<IAudioSettingsService>(this);
			yield return null;
			yield break;
		}

		protected float RealtimeDelta
		{
			get
			{
				return Time.realtimeSinceStartup - this.previousRealtimeSinceStartup;
			}
		}

		public override IEnumerator BindServices()
		{
			yield return base.BindServices();
			bool activateAudioManager = Amplitude.Unity.Framework.Application.Registry.GetValue<bool>(AudioManager.Registers.Enabled, true);
			base.SetLastError(0, "Initializing Fmod...");
			bool fmodError = false;
			if (activateAudioManager)
			{
				Diagnostics.Log("The audio manager is activated: creating the fmod event system...");
				fmodError |= this.ErrorCheck(Event_Factory.EventSystem_Create(ref this.fmodEventSystem));
			}
			if (activateAudioManager && !fmodError)
			{
				Diagnostics.Assert(this.fmodEventSystem != null, "Fmod event system must be non-null if there is no errors.");
				INITFLAGS fmodInitFlags = INITFLAGS.VOL0_BECOMES_VIRTUAL;
				Diagnostics.Log("Setting up the fmod system...");
				Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
				fmodError |= this.ErrorCheck(this.fmodEventSystem.getSystemObject(ref this.fmodSystem));
				Diagnostics.Assert(this.fmodSystem != null, "Fmod system must be non-null.");
				fmodError |= this.ErrorCheck(this.fmodSystem.setSpeakerMode(SPEAKERMODE.STEREO));
				Diagnostics.Assert(this.fmodSystem != null, "Fmod system must be non-null.");
				fmodError |= this.ErrorCheck(this.fmodSystem.setSoftwareChannels(this.NumSoftwareChannels));
				Diagnostics.Log("Initializing the fmod event system...");
				Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
				fmodError |= this.ErrorCheck(this.fmodEventSystem.init(this.MaxVirtualChannels, fmodInitFlags, (IntPtr)null, EVENT_INITFLAGS.NORMAL));
				string path = null;
				if (!string.IsNullOrEmpty(AudioManager.Preferences.FmodMediaPath))
				{
					path = Amplitude.Unity.Framework.Path.GetFullPath(AudioManager.Preferences.FmodMediaPath);
					if (path != null && !path.EndsWith("/"))
					{
						path += '/';
					}
				}
				if (!string.IsNullOrEmpty(path))
				{
					Diagnostics.Log("Load fmod data from folder '{0}'.", new object[]
					{
						path
					});
					Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
					if (System.IO.Path.IsPathRooted(path))
					{
						string relativePath = Amplitude.Unity.Framework.Path.MakeRelativeToAssetPath(path);
						Diagnostics.Log(string.Format("Rooted path '{0}' has been made relative into '{1}'.", path, relativePath));
						path = relativePath;
					}
					fmodError |= this.ErrorCheck(this.fmodEventSystem.setMediaPath(path));
					Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
					fmodError |= this.ErrorCheck(this.fmodEventSystem.load(this.FmodDesignerFileName));
					Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
					fmodError |= this.ErrorCheck(this.fmodEventSystem.getMusicSystem(ref this.fmodMusicSystem));
					if (this.EventGroupsToPreload != null)
					{
						for (int index = 0; index < this.EventGroupsToPreload.Length; index++)
						{
							this.LoadEventData(this.EventGroupsToPreload[index]);
						}
					}
					this.fmodEventMasterCategory = this.GetCategoryByFmodName(this.masterCategoryFmodName);
					fmodError |= (this.fmodEventMasterCategory == null);
				}
				else
				{
					Diagnostics.LogError("Fmod media path is invalid({0}), can't load data.", new object[]
					{
						AudioManager.Preferences.FmodMediaPath
					});
					fmodError = true;
				}
				if (fmodError)
				{
					Diagnostics.LogError("Fmod initialization failed.");
					if (this.fmodEventSystem != null)
					{
						this.fmodEventSystem.release();
					}
					if (AudioManager.Preferences.ActivateAudioProfiler)
					{
						NetEventSystem.shutDown();
					}
				}
				else
				{
					this.fmodSystemInitialized = true;
				}
			}
			this.nullAudioManager = new NullAudioManager();
			yield return this.BindAudioSettingsService();
			yield return this.BindAudioEventService();
			yield return this.BindLayeredMusicServices();
			yield return this.BindMusicServices();
			if (this.fmodSystemInitialized)
			{
				Services.AddService<IAudioService>(this);
			}
			else
			{
				Diagnostics.LogWarning("The Fmod initialization has bound the null audio service.");
				Services.AddService<IAudioService>(this.nullAudioManager);
			}
			yield break;
		}

		public void FreeEventData(StaticString groupName)
		{
			if (groupName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Failed to free fmod group event data, group name is empty.");
				return;
			}
			EventGroup eventGroup = null;
			string fmodPath = this.GetFmodPath(groupName);
			Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
			this.ErrorCheck(this.fmodEventSystem.getGroup(fmodPath, false, ref eventGroup));
			if (eventGroup == null)
			{
				Diagnostics.LogWarning("Failed to free fmod group event data, group <{0}> does not exist.", new object[]
				{
					groupName
				});
				return;
			}
			eventGroup.freeEventData();
		}

		public EventCategory GetCategoryByFmodName(string categoryFmodName)
		{
			if (string.IsNullOrEmpty(categoryFmodName))
			{
				Diagnostics.LogWarning("Failed to find category, category fmod name is empty.");
				return null;
			}
			EventCategory eventCategory = null;
			Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
			this.ErrorCheck(this.fmodEventSystem.getCategory(categoryFmodName, ref eventCategory));
			if (eventCategory == null)
			{
				Diagnostics.LogWarning("Failed to retrieve the category by fmodName: <" + categoryFmodName + ">).");
			}
			return eventCategory;
		}

		public EventGroup LoadEventData(StaticString groupName)
		{
			if (groupName == StaticString.Empty)
			{
				Diagnostics.LogWarning("Failed to load event group data, group name is empty.");
				return null;
			}
			EventGroup eventGroup = null;
			string fmodPath = this.GetFmodPath(groupName);
			Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
			this.ErrorCheck(this.fmodEventSystem.getGroup(fmodPath, true, ref eventGroup));
			if (eventGroup == null)
			{
				Diagnostics.LogWarning("Failed to load fmod event group <" + groupName + ">.");
			}
			return eventGroup;
		}

		public int RegisterListener(AudioReceiver listener)
		{
			if (listener == null)
			{
				Diagnostics.LogError("Can't register null listener.");
				return -1;
			}
			Diagnostics.Assert(this.audioListeners != null, "this.audioListeners == null");
			int num = this.audioListeners.IndexOf(listener);
			if (num >= 0)
			{
				Diagnostics.LogWarning("Listener <" + num + "> already registered.");
				return num;
			}
			int num2 = this.audioListeners.Count + 1;
			Diagnostics.Assert(this.fmodEventSystem != null, "fmodEventSystem == null.");
			if (this.ErrorCheck(this.fmodEventSystem.set3DNumListeners(num2)))
			{
				return -1;
			}
			Diagnostics.Assert(this.audioListeners != null, "this.audioListeners == null");
			this.audioListeners.Add(listener);
			Diagnostics.Log("New audio listener registered: " + num2 + " listener(s) registered.");
			return this.audioListeners.IndexOf(listener);
		}

		public void Set3DListenerAttributes(int listenerId, Vector3 position, Vector3 velocity, Vector3 forward, Vector3 up)
		{
			if (listenerId == -1)
			{
				Diagnostics.LogError("Can't set 3d listener attributes, the listener id is invalid.");
				return;
			}
			VECTOR vector = position.ToFmodVector();
			VECTOR vector2 = velocity.ToFmodVector();
			VECTOR vector3 = forward.ToFmodVector();
			VECTOR vector4 = up.ToFmodVector();
			Diagnostics.Assert(this.fmodEventSystem != null);
			this.ErrorCheck(this.fmodEventSystem.set3DListenerAttributes(listenerId, ref vector, ref vector2, ref vector3, ref vector4));
		}

		public void SetAmbientReverb(StaticString reverbPresetName)
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			if (this.fmodEventSystem == null)
			{
				return;
			}
			int num = 0;
			REVERB_PROPERTIES reverb_PROPERTIES = default(REVERB_PROPERTIES);
			this.ErrorCheck(this.fmodEventSystem.getReverbPreset(reverbPresetName, ref reverb_PROPERTIES, ref num));
			this.ErrorCheck(this.fmodEventSystem.setReverbAmbientProperties(ref reverb_PROPERTIES));
		}

		public void SetCategoryVolume(string categoryFmodName, float volume, float fadeDuration = 0f)
		{
			EventCategory categoryByFmodName = this.GetCategoryByFmodName(categoryFmodName);
			if (categoryByFmodName == null)
			{
				Diagnostics.LogError("Can't change the volume of category {0}", new object[]
				{
					categoryFmodName
				});
				return;
			}
			Diagnostics.Assert(this.categoryVolumeModifiers != null);
			for (int i = 0; i < this.categoryVolumeModifiers.Count; i++)
			{
				if (this.categoryVolumeModifiers[i].Category.getRaw() == categoryByFmodName.getRaw())
				{
					this.categoryVolumeModifiers.RemoveAt(i);
					i--;
				}
			}
			this.categoryVolumeModifiers.Add(new AudioManager.CategoryVolumeModifier(categoryByFmodName, volume, fadeDuration));
		}

		protected virtual void LateUpdate()
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			if (this.fmodSystem == null)
			{
				return;
			}
			float realtimeDelta = this.RealtimeDelta;
			float realtimeSinceStartup = Time.realtimeSinceStartup;
			this.UpdateEventLayerSystem(realtimeSinceStartup, realtimeDelta);
			Diagnostics.Assert(this.fmodEventSystem != null);
			this.ErrorCheck(this.fmodEventSystem.update());
			if (this.fmodProfilingEnabled)
			{
				this.ErrorCheck(NetEventSystem.update());
			}
			this.previousRealtimeSinceStartup = Time.realtimeSinceStartup;
		}

		protected void OnApplicationFocus(bool focus)
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			this.masterMuteWhenLostFocus = !focus;
			this.ApplyMasterMute();
		}

		protected override void Releasing()
		{
			base.Releasing();
			this.ReleaseFmodSystem();
		}

		protected virtual void Update()
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			Diagnostics.Assert(this.categoryVolumeModifiers != null);
			for (int i = 0; i < this.categoryVolumeModifiers.Count; i++)
			{
				AudioManager.CategoryVolumeModifier categoryVolumeModifier = this.categoryVolumeModifiers[i];
				Diagnostics.Assert(categoryVolumeModifier != null);
				categoryVolumeModifier.UpdateVolume(this.RealtimeDelta);
				if (categoryVolumeModifier.IsFinished)
				{
					this.categoryVolumeModifiers.RemoveAt(i);
					i--;
				}
			}
		}

		protected bool ErrorCheck(RESULT result)
		{
			base.SetLastError((int)result);
			if (result != RESULT.OK && result != RESULT.ERR_EVENT_FAILED)
			{
				if (result == RESULT.ERR_EVENT_NOTFOUND)
				{
					Diagnostics.LogWarning("Fmod error reported: {0} - {1}", new object[]
					{
						result,
						Error.String(result)
					});
				}
				else
				{
					Diagnostics.LogError("Fmod error reported: {0} - {1}", new object[]
					{
						result,
						Error.String(result)
					});
				}
				return true;
			}
			return false;
		}

		private void OnDestroy()
		{
			this.ReleaseFmodSystem();
		}

		private void ReleaseFmodSystem()
		{
			if (!this.fmodSystemInitialized)
			{
				return;
			}
			Diagnostics.Log("Releasing fmod system.");
			this.fmodSystemInitialized = false;
			if (this.fmodEventSystem != null)
			{
				this.ErrorCheck(this.fmodEventSystem.release());
			}
			if (this.fmodProfilingEnabled)
			{
				this.ErrorCheck(NetEventSystem.shutDown());
			}
		}

		protected const float VolumeLayerMaximumValue = 1f;

		protected const float VolumeLayerMinimumValue = 0f;

		private Dictionary<StaticString, LayeredEvent> currentLayeredEventByNames = new Dictionary<StaticString, LayeredEvent>();

		private List<LayeredEvent> currentLayeredEvents = new List<LayeredEvent>();

		private ReadOnlyCollection<LayeredEvent> readOnlyLayeredEvents;

		private IDatabase<LayeredEventDefinition> layeredEventDatabase;

		protected StaticString eventLayerVolumeName = "Volume";

		private AudioManager.Music lastPlayedMusic;

		private List<AudioManager.Music> currentLayeredMusicDefinitions = new List<AudioManager.Music>();

		private Dictionary<uint, MusicPrompt> fmodMusicPrompts = new Dictionary<uint, MusicPrompt>();

		[SerializeField]
		private string ambianceCategoryFmodName = "ambiance";

		private bool ambianceMute;

		private float ambianceVolume = 1f;

		private EventCategory fmodEventAmbianceCategory;

		private EventCategory fmodEventGUICategory;

		private EventCategory fmodEventMasterCategory;

		private EventCategory fmodEventMusicCategory;

		private EventCategory fmodEventSFXCategory;

		[SerializeField]
		private string guiCategoryFmodName = "gui";

		private bool guiMute;

		private float guiVolume = 1f;

		[SerializeField]
		private string masterCategoryFmodName = "master";

		private bool masterMute;

		private bool masterMuteWhenLostFocus;

		private float masterVolume = 1f;

		[SerializeField]
		private string musicCategoryFmodName = "music";

		private bool musicMute;

		private float musicVolume = 1f;

		private float musicIntervalDurationRatio = 1f;

		private bool muteWhenLostFocus;

		[SerializeField]
		private string sfxCategoryFmodName = "sfx";

		private bool sfxMute;

		private float sfxVolume = 1f;

		public string FmodDesignerFileName;

		public string FmodProjectName;

		public int MaxVirtualChannels = 1024;

		public int NumSoftwareChannels = 128;

		public string[] EventGroupsToPreload;

		protected bool fmodSystemInitialized;

		private readonly List<AudioReceiver> audioListeners = new List<AudioReceiver>();

		private readonly List<AudioManager.CategoryVolumeModifier> categoryVolumeModifiers = new List<AudioManager.CategoryVolumeModifier>();

		private System fmodSystem;

		private EventSystem fmodEventSystem;

		private MusicSystem fmodMusicSystem;

		private bool fmodProfilingEnabled;

		private NullAudioManager nullAudioManager;

		private float previousRealtimeSinceStartup;

		private class Music
		{
			public Music(LayeredEvent layeredEvent, int priority)
			{
				this.LayeredEvent = layeredEvent;
				this.Priority = priority;
			}

			public StaticString Name
			{
				get
				{
					Diagnostics.Assert(this.LayeredEvent != null);
					return this.LayeredEvent.Name;
				}
			}

			public readonly LayeredEvent LayeredEvent;

			public int Priority;
		}

		public abstract class Preferences
		{
			public static bool ActivateAudioManager
			{
				get
				{
					return AudioManager.Preferences.activateAudioManager;
				}
			}

			public static bool ActivateAudioProfiler
			{
				get
				{
					return AudioManager.Preferences.activateAudioProfiler;
				}
			}

			public static bool MuteWhenLostFocus
			{
				get
				{
					return AudioManager.Preferences.muteWhenLostFocus;
				}
			}

			public static bool MuteMusic
			{
				get
				{
					return AudioManager.Preferences.muteMusic;
				}
			}

			public static string FmodMediaPath
			{
				get
				{
					return AudioManager.Preferences.fmodMediaPath;
				}
			}

			private static bool activateAudioManager = true;

			private static bool activateAudioProfiler;

			private static bool muteWhenLostFocus = true;

			private static bool muteMusic;

			private static string fmodMediaPath = "Public/Audio";
		}

		public static class Registers
		{
			public static StaticString Enabled = new StaticString("Settings/Audio/Enabled");

			public static StaticString MuteWhenLostFocus = new StaticString("Settings/Audio/MuteWhenLostFocus");

			public static StaticString AmbianceMute = new StaticString("Settings/Audio/AudioCategory/Ambiance/Mute");

			public static StaticString AmbianceVolume = new StaticString("Settings/Audio/AudioCategory/Ambiance/Volume");

			public static StaticString GUIMute = new StaticString("Settings/Audio/AudioCategory/GUI/Mute");

			public static StaticString GUIVolume = new StaticString("Settings/Audio/AudioCategory/GUI/Volume");

			public static StaticString MasterMute = new StaticString("Settings/Audio/AudioCategory/Master/Mute");

			public static StaticString MasterVolume = new StaticString("Settings/Audio/AudioCategory/Master/Volume");

			public static StaticString MusicMute = new StaticString("Settings/Audio/AudioCategory/Music/Mute");

			public static StaticString MusicVolume = new StaticString("Settings/Audio/AudioCategory/Music/Volume");

			public static StaticString MusicIntervalDurationRatio = new StaticString("Settings/Audio/AudioCategory/Music/IntervalDurationRatio");

			public static StaticString SFXMute = new StaticString("Settings/Audio/AudioCategory/SFX/Mute");

			public static StaticString SFXVolume = new StaticString("Settings/Audio/AudioCategory/SFX/Volume");
		}

		private class CategoryVolumeModifier
		{
			public CategoryVolumeModifier(EventCategory category, float wantedVolume, float fadeDuration)
			{
				if (category == null)
				{
					throw new ArgumentNullException("category");
				}
				this.category = category;
				this.wantedVolume = wantedVolume;
				float num = 0f;
				category.getVolume(ref num);
				this.currentVolume = num;
				if (Math.Abs(fadeDuration) < 1.401298E-45f)
				{
					this.deltaVolume = float.PositiveInfinity;
				}
				else
				{
					this.deltaVolume = 1f / fadeDuration;
				}
			}

			public EventCategory Category
			{
				get
				{
					return this.category;
				}
			}

			public bool IsFinished
			{
				get
				{
					return Math.Abs(this.currentVolume - this.wantedVolume) < float.Epsilon;
				}
			}

			public void UpdateVolume(float deltaTime)
			{
				if (float.IsPositiveInfinity(this.deltaVolume))
				{
					this.currentVolume = this.wantedVolume;
				}
				else
				{
					float num = (this.currentVolume >= this.wantedVolume) ? (-this.deltaVolume) : this.deltaVolume;
					num *= deltaTime;
					this.currentVolume += num;
					if (Mathf.Abs(this.currentVolume - this.wantedVolume) <= num)
					{
						this.currentVolume = this.wantedVolume;
					}
				}
				Diagnostics.Assert(this.category != null);
				this.category.setVolume(this.currentVolume);
			}

			private readonly EventCategory category;

			private readonly float deltaVolume;

			private readonly float wantedVolume;

			private float currentVolume;
		}
	}
}
