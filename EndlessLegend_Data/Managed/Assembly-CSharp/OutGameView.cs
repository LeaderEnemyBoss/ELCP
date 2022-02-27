using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity;
using Amplitude.Unity.Audio;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Gui;
using Amplitude.Unity.View;
using UnityEngine;
using UnityEngine.SceneManagement;

public class OutGameView : View
{
	public OutGameView()
	{
		base.Static = true;
	}

	public GameObject SceneObject { get; private set; }

	public override void Activate(bool active, params object[] parameters)
	{
		base.Activate(active, parameters);
		if (SystemInfo.graphicsShaderLevel < 30)
		{
			Diagnostics.LogError("Insufficient hardware. UnityEngine.SystemInfo.graphicsShaderLevel = {0}. Should be higher or equal than {1}", new object[]
			{
				SystemInfo.graphicsShaderLevel,
				30
			});
		}
		Amplitude.Unity.Audio.IAudioLayeredMusicService service = Services.GetService<Amplitude.Unity.Audio.IAudioLayeredMusicService>();
		if (service != null)
		{
			service.StopAllMusics();
		}
		if (active)
		{
			this.LoadScene();
		}
		else
		{
			this.UnloadScene();
		}
	}

	public override void Focus(bool focused)
	{
		base.Focus(focused);
		ICameraService service = Services.GetService<ICameraService>();
		if (service != null)
		{
			service.Camera.gameObject.SetActive(!focused);
		}
		if (this.SceneObject != null)
		{
			this.SceneObject.SetActive(focused);
		}
		Amplitude.Unity.Gui.IGuiService service2 = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
		if (this.Parameters != null && this.Parameters.Length > 0)
		{
			string text = this.Parameters[0] as string;
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = text;
				if (text2 != null)
				{
					if (OutGameView.<>f__switch$map20 == null)
					{
						OutGameView.<>f__switch$map20 = new Dictionary<string, int>(1)
						{
							{
								"GameEnded",
								0
							}
						};
					}
					int num;
					if (OutGameView.<>f__switch$map20.TryGetValue(text2, out num))
					{
						if (num == 0)
						{
							Diagnostics.Assert(this.Parameters.Length == 2);
							Diagnostics.Assert(this.Parameters[1] is EmpireInfo[]);
							service2.GetGuiPanel<MenuScoreScreen>().Show(new object[]
							{
								this.Parameters[1]
							});
							return;
						}
					}
				}
			}
		}
		if (service2 != null)
		{
			if (focused)
			{
				service2.Show("01-MenuMainScreen", new object[0]);
			}
			else
			{
				service2.Hide("01-MenuMainScreen");
			}
		}
	}

	public override void Reactivate(params object[] parameters)
	{
		base.Reactivate(parameters);
		if (this.loadLevelAsyncOperation == null)
		{
			Amplitude.Unity.Gui.IGuiService service = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
			service.Hide(typeof(LoadingScreen));
		}
	}

	private void LoadScene()
	{
		if (this.SceneObject == null)
		{
			if (this.loadLevelAsyncOperation != null)
			{
				return;
			}
			if (this.LevelNames != null && this.LevelNames.Length != 0)
			{
				this.LevelNames = this.LevelNames.Distinct<string>().ToArray<string>();
				string text = this.LevelNames[UnityEngine.Random.Range(0, this.LevelNames.Length)];
				if (!string.IsNullOrEmpty(this.lastLevelName))
				{
					text = this.lastLevelName;
				}
				else if (string.IsNullOrEmpty(text))
				{
					foreach (string text2 in this.LevelNames)
					{
						if (!string.IsNullOrEmpty(text2))
						{
							text = text2;
							break;
						}
					}
				}
				if (text != null)
				{
					if (this.KeepSameLevel)
					{
						this.lastLevelName = text;
					}
					UnityCoroutine.StartCoroutine(this, this.LoadSceneAsync(text), null);
				}
			}
		}
	}

	private void UnloadScene()
	{
		if (this.SceneObject != null)
		{
			Amplitude.Unity.View.IViewService service = Services.GetService<Amplitude.Unity.View.IViewService>();
			if (service.PendingView != null && this != service.PendingView)
			{
				Diagnostics.Log("Destroying the outgame view scene to free some resources.\n(switching to '{0}')", new object[]
				{
					service.PendingView.GetType().ToString()
				});
				UnityEngine.Object.Destroy(this.SceneObject);
				this.SceneObject = null;
				Resources.UnloadUnusedAssets();
			}
		}
	}

	private IEnumerator LoadSceneAsync(string levelName)
	{
		if (this.loadLevelAsyncOperation != null)
		{
			yield break;
		}
		Amplitude.Unity.Gui.IGuiService guiService = Services.GetService<Amplitude.Unity.Gui.IGuiService>();
		LoadingScreen loadingScreen = guiService.GetGuiPanel<LoadingScreen>();
		if (loadingScreen != null)
		{
			object dontDisplayAnyLoadingTip = new LoadingScreen.DontDisplayAnyLoadingTip();
			loadingScreen.Show(new object[]
			{
				dontDisplayAnyLoadingTip
			});
		}
		Diagnostics.Log("Loading the outgame view...");
		Diagnostics.Progress.SetProgress(1f, "%GameClientStateLoadingOutgameView");
		this.loadLevelAsyncOperation = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
		while (!this.loadLevelAsyncOperation.isDone)
		{
			yield return null;
		}
		this.SceneObject = GameObject.Find("[OutGameView_Layout]");
		if (this.SceneObject == null)
		{
			Diagnostics.LogError("Cannot locate the root object '{0}' after loading the scene '{1}'.", new object[]
			{
				"[OutGameView_Layout]",
				levelName
			});
		}
		else
		{
			GameObject sceneObject = this.SceneObject;
			sceneObject.name += "(scene: additive, async)";
			this.SceneObject.transform.parent = base.transform;
			this.SceneObject.transform.localPosition = Vector3.zero;
			this.SceneObject.transform.localRotation = Quaternion.identity;
			this.SceneObject.SetActive(true);
		}
		this.loadLevelAsyncOperation = null;
		Resources.UnloadUnusedAssets();
		if (Amplitude.Unity.Framework.Application.Bootstrapper != null)
		{
			Amplitude.Unity.Framework.Application.Bootstrapper.gameObject.SetActive(false);
		}
		guiService.Hide(typeof(LoadingScreen));
		if (this.Parameters.Length == 1)
		{
			Type typeOfPanelToShow = this.Parameters[0] as Type;
			Diagnostics.Assert(typeOfPanelToShow != null);
			guiService.Show(typeOfPanelToShow, new object[0]);
		}
		Amplitude.Unity.Audio.IAudioLayeredMusicService musicService = Services.GetService<Amplitude.Unity.Audio.IAudioLayeredMusicService>();
		musicService.PlayLayeredMusic("OutGameMusic", OutGameView.OutGameMusicName, 1);
		yield break;
	}

	private const string LayoutName = "[OutGameView_Layout]";

	private const string MenuMainScreenName = "01-MenuMainScreen";

	public static readonly StaticString OutGameMusicName = "OutGameMusic";

	public string[] LevelNames;

	public bool KeepSameLevel;

	private AsyncOperation loadLevelAsyncOperation;

	private string lastLevelName;
}
