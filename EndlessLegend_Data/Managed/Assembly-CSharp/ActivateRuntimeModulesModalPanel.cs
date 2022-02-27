using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Steam;
using UnityEngine;

public class ActivateRuntimeModulesModalPanel : GuiPlayerControllerModalPanel
{
	private string[] RuntimeModuleNames { get; set; }

	private string[] MissingWorkshopModuleNames { get; set; }

	private string[] MissingNonWorkshopModuleNames { get; set; }

	private object LobbyData { get; set; }

	private GuiPanel PanelToShowOnCancel { get; set; }

	private bool IsInvalid
	{
		get
		{
			if (this.MissingNonWorkshopModuleNames.Length > 0)
			{
				return true;
			}
			int i = 0;
			int count = this.guiModules.Count;
			while (i < count)
			{
				if (this.guiModules[i].IsInvalid)
				{
					return true;
				}
				i++;
			}
			return false;
		}
	}

	private RuntimeModuleConfigurationState RuntimeModuleConfigurationState { get; set; }

	public override bool HandleCancelRequest()
	{
		if (this.PanelToShowOnCancel != null)
		{
			this.PanelToShowOnCancel.Show(new object[0]);
		}
		this.Hide(false);
		return true;
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.RefreshAvailableRuntimeModules();
		this.guiModules.Clear();
		this.ValidateButton.AgeTransform.Enable = false;
		this.NoModulesLabel.Visible = false;
		IRuntimeModuleSubscriptionService service = Services.GetService<IRuntimeModuleSubscriptionService>();
		if (service != null)
		{
			string[] missingWorkshopModuleNames;
			string[] missingNonWorkshopModuleNames;
			service.GetRuntimeModuleListState(this.RuntimeModuleNames, out missingWorkshopModuleNames, out missingNonWorkshopModuleNames);
			this.MissingWorkshopModuleNames = missingWorkshopModuleNames;
			this.MissingNonWorkshopModuleNames = missingNonWorkshopModuleNames;
		}
		List<ulong> list = new List<ulong>();
		int i = 0;
		int count = service.Downloads.Count;
		while (i < count)
		{
			ulong num = service.Downloads[i];
			string value = global::RuntimeManager.Folders.Workshop.Affix + num;
			if (this.RuntimeModuleNames.Contains(value) && !service.IsInstalled(num))
			{
				list.Add(num);
			}
			i++;
		}
		if (this.MissingWorkshopModuleNames.Length == 0 && this.MissingNonWorkshopModuleNames.Length == 0 && list.Count == 0)
		{
			int j = 0;
			int count2 = this.availableRuntimeModules.Count;
			while (j < count2)
			{
				if (this.RuntimeModuleNames.Contains(this.availableRuntimeModules[j].Name))
				{
					this.guiModules.Add(new GuiModule(this.availableRuntimeModules[j] as RuntimeModuleEx));
				}
				j++;
			}
			if (this.guiModules.Count == 0)
			{
				this.InfosLabel.Text = AgeLocalizer.Instance.LocalizeString("%ActivateModulesInfosVanillaTitle");
				this.NoModulesLabel.Visible = true;
			}
			else
			{
				this.InfosLabel.Text = AgeLocalizer.Instance.LocalizeString("%ActivateModulesInfosActivateTitle");
			}
			this.ValidateButton.AgeTransform.Enable = true;
		}
		else
		{
			int k = 0;
			int count3 = list.Count;
			while (k < count3)
			{
				this.guiModules.Add(new GuiModule(list[k]));
				k++;
			}
			int l = 0;
			int num2 = this.MissingWorkshopModuleNames.Length;
			while (l < num2)
			{
				ulong item = ulong.Parse(this.MissingWorkshopModuleNames[l].Replace(global::RuntimeManager.Folders.Workshop.Affix, string.Empty));
				if (!list.Contains(item))
				{
					this.guiModules.Add(new GuiModule(this.MissingWorkshopModuleNames[l]));
				}
				l++;
			}
			int m = 0;
			int num3 = this.MissingNonWorkshopModuleNames.Length;
			while (m < num3)
			{
				this.guiModules.Add(new GuiModule(this.MissingNonWorkshopModuleNames[m]));
				m++;
			}
			if (this.IsInvalid)
			{
				this.InfosLabel.Text = AgeLocalizer.Instance.LocalizeString("%ActivateModulesInfosInvalidTitle");
			}
			else
			{
				this.InfosLabel.Text = AgeLocalizer.Instance.LocalizeString("%ActivateModulesInfosDownloadingTitle");
			}
		}
		this.SortGuiModules();
		this.GuiModulesTable.Height = 0f;
		this.GuiModulesTable.ReserveChildren(this.guiModules.Count, this.ModuleLinePrefab, "ExternalModuleLine");
		this.GuiModulesTable.RefreshChildrenIList<GuiModule>(this.guiModules, this.setupGuiModuleDelegate, true, false);
		this.GuiModulesTable.ArrangeChildren();
		this.GuiModulesScrollView.OnPositionRecomputed();
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		this.PanelToShowOnCancel = null;
		this.RuntimeModuleNames = null;
		this.MissingNonWorkshopModuleNames = null;
		this.MissingWorkshopModuleNames = null;
		IRuntimeModuleSubscriptionService subscriptionService = Services.GetService<IRuntimeModuleSubscriptionService>();
		if (subscriptionService != null)
		{
			subscriptionService.ItemDownloadProgress -= this.RuntimeModuleSubscriptionService_ItemDownloadProgress;
		}
		ISteamUGCService steamUGCService = Services.GetService<ISteamUGCService>();
		if (steamUGCService != null)
		{
			steamUGCService.SteamUGCRemoteStoragePublishedFileSubscribed -= this.SteamUGCService_SteamUGCRemoteStoragePublishedFileSubscribed;
		}
		IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
		if (runtimeService != null)
		{
			runtimeService.RuntimeModuleDatabaseUpdate -= this.RuntimeService_RuntimeModuleDatabaseUpdate;
		}
		yield break;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.setupGuiModuleDelegate = new AgeTransform.RefreshTableItem<GuiModule>(this.SetupGuiModule);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		IRuntimeModuleSubscriptionService subscriptionService = Services.GetService<IRuntimeModuleSubscriptionService>();
		if (subscriptionService != null)
		{
			subscriptionService.ItemDownloadProgress += this.RuntimeModuleSubscriptionService_ItemDownloadProgress;
		}
		ISteamUGCService steamUGCService = Services.GetService<ISteamUGCService>();
		if (steamUGCService != null)
		{
			steamUGCService.SteamUGCRemoteStoragePublishedFileSubscribed += this.SteamUGCService_SteamUGCRemoteStoragePublishedFileSubscribed;
		}
		IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
		if (runtimeService != null)
		{
			runtimeService.RuntimeModuleDatabaseUpdate += this.RuntimeService_RuntimeModuleDatabaseUpdate;
		}
		if (parameters.Length >= 2)
		{
			this.RuntimeModuleNames = (parameters[0] as string[]);
			this.LobbyData = parameters[1];
			if (parameters.Length > 2)
			{
				this.PanelToShowOnCancel = (parameters[2] as GuiPanel);
			}
			IRuntimeModuleSubscriptionService runtimeModuleSubscriptionService = Services.GetService<IRuntimeModuleSubscriptionService>();
			if (runtimeModuleSubscriptionService != null)
			{
				string[] missingWorkshopItems;
				string[] missingNonWorkshopItems;
				runtimeModuleSubscriptionService.GetRuntimeModuleListState(this.RuntimeModuleNames, out missingWorkshopItems, out missingNonWorkshopItems);
				this.MissingWorkshopModuleNames = missingWorkshopItems;
				this.MissingNonWorkshopModuleNames = missingNonWorkshopItems;
			}
			int i = 0;
			int lth = this.MissingWorkshopModuleNames.Length;
			while (i < lth)
			{
				ulong publishedFileId = ulong.Parse(this.MissingWorkshopModuleNames[i].Replace(global::RuntimeManager.Folders.Workshop.Affix, string.Empty));
				if (!runtimeModuleSubscriptionService.IsSubscribed(publishedFileId))
				{
					runtimeModuleSubscriptionService.Subscribe(publishedFileId);
				}
				i++;
			}
			this.RefreshContent();
		}
		else
		{
			this.HandleCancelRequest();
		}
		yield break;
	}

	protected override void OnUnload()
	{
		this.setupGuiModuleDelegate = null;
		base.OnUnload();
	}

	private void OnCancelCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	private void OnValidateCB(GameObject obj)
	{
		IRuntimeService service = Services.GetService<IRuntimeService>();
		IRuntimeModulePlaylistService service2 = Services.GetService<IRuntimeModulePlaylistService>();
		List<RuntimeModuleConfiguration> list = new List<RuntimeModuleConfiguration>();
		if (this.guiModules.Count == 0 || this.guiModules[0].Module.Type != RuntimeModuleType.Standalone)
		{
			list.Add(new RuntimeModuleConfiguration(service.VanillaModuleName));
		}
		int i = 0;
		int count = this.guiModules.Count;
		while (i < count)
		{
			list.Add(new RuntimeModuleConfiguration(this.guiModules[i].Module.Name));
			i++;
		}
		ModulePlaylist anonymousModulePlaylist = ModulePlaylist.GetAnonymousModulePlaylist(list.ToArray());
		Amplitude.Unity.Framework.Application.Registry.SetValue(global::Application.Registers.LastModulePlaylistActivated, anonymousModulePlaylist.Name);
		Amplitude.Unity.Framework.Application.Registry.SetValue(global::Application.Registers.AnonymousModulePlaylist, ModulePlaylist.GetConfigurationUrl(anonymousModulePlaylist.Configuration));
		service2.CurrentModulePlaylist = anonymousModulePlaylist;
		global::RuntimeManager.LobbyData = this.LobbyData;
		service.ReloadRuntime(anonymousModulePlaylist.Configuration);
		this.Hide(false);
		Diagnostics.Progress.Clear();
		Diagnostics.Progress.SetProgress(0.9f, "%LoadingRuntimeModules");
		object obj2 = new LoadingScreen.DontDisplayAnyLoadingTip();
		base.GuiService.Show(typeof(LoadingScreen), new object[]
		{
			"GUI/DynamicBitmaps/Backdrop/Pov_Auriga",
			obj2
		});
	}

	private void RefreshAvailableRuntimeModules()
	{
		IDatabase<RuntimeModule> database = Databases.GetDatabase<RuntimeModule>(true);
		IRuntimeService runtimeService = Services.GetService<IRuntimeService>();
		RuntimeModule runtimeModule = null;
		if (runtimeService != null)
		{
			runtimeModule = database.FirstOrDefault((RuntimeModule module) => module.Type == RuntimeModuleType.Standalone && module.Name == runtimeService.VanillaModuleName);
		}
		this.availableRuntimeModules = database.ToList<RuntimeModule>();
		if (runtimeModule != null)
		{
			this.availableRuntimeModules.Remove(runtimeModule);
		}
	}

	private void RuntimeService_RuntimeModuleDatabaseUpdate(object sender, RuntimeModuleDatabaseUpdateEventArgs e)
	{
		this.RefreshContent();
	}

	private void RuntimeModuleSubscriptionService_ItemDownloadProgress(object sender, ItemDownloadProgressEventArgs e)
	{
		int i = 0;
		int count = this.guiModules.Count;
		while (i < count)
		{
			if (this.guiModules[i].IsDownloading && this.guiModules[i].DownloadingModuleID == e.PublishedFileId)
			{
				this.guiModules[i].ModuleLine.RefreshDownloadProgress(e.BytesDownloaded, e.BytesTotal);
			}
			i++;
		}
	}

	private void SetupGuiModule(AgeTransform tableItem, GuiModule guiModule, int index)
	{
		ExternalModuleLine component = tableItem.GetComponent<ExternalModuleLine>();
		if (component == null)
		{
			Diagnostics.LogError("In the MenuModuleSetupScreen, trying to refresh a table item that is not a ExternalModuleLine");
			return;
		}
		if (index % 20 == 0)
		{
			tableItem.StartNewMesh = true;
		}
		if (guiModule.IsInvalid)
		{
			component.SetInvalidMode(guiModule);
		}
		else if (guiModule.IsDownloading)
		{
			component.SetDownloadingMode(guiModule);
		}
		else
		{
			component.RefreshContent(guiModule, base.gameObject, index, true);
		}
		guiModule.ModuleLine = component;
	}

	private void SortGuiModules()
	{
		List<GuiModule> list = new List<GuiModule>();
		int i = 0;
		int count = this.guiModules.Count;
		if (this.RuntimeModuleNames != null && this.RuntimeModuleNames.Length != 0)
		{
			Diagnostics.Assert(count == this.RuntimeModuleNames.Length - 1, string.Format("ELCP: Module Length Mismatch: guiModules {0}, RuntimeModuleNames {1}", count, this.RuntimeModuleNames.Length - 1));
			string[] runtimeModuleNames = this.RuntimeModuleNames;
			for (int j = 0; j < runtimeModuleNames.Length; j++)
			{
				string name = runtimeModuleNames[j];
				GuiModule guiModule = this.guiModules.Find((GuiModule Module) => Module.Name == name);
				if (guiModule != null)
				{
					list.Add(guiModule);
				}
			}
		}
		else
		{
			while (i < count)
			{
				if (this.guiModules[i].IsDownloading || this.guiModules[i].IsInvalid || this.guiModules[i].Module == null)
				{
					list.Add(this.guiModules[i]);
					break;
				}
				i++;
			}
			int k = 0;
			int count2 = this.guiModules.Count;
			while (k < count2)
			{
				if (!this.guiModules[k].IsDownloading && !this.guiModules[k].IsInvalid && this.guiModules[k].Module != null && this.guiModules[k].Module.Type == RuntimeModuleType.Standalone)
				{
					list.Add(this.guiModules[k]);
					break;
				}
				k++;
			}
			int l = 0;
			int count3 = this.guiModules.Count;
			while (l < count3)
			{
				if (!this.guiModules[l].IsDownloading && !this.guiModules[l].IsInvalid && this.guiModules[l].Module != null && this.guiModules[l].Module.Type == RuntimeModuleType.Conversion)
				{
					list.Add(this.guiModules[l]);
					break;
				}
				l++;
			}
			int m = 0;
			int count4 = this.guiModules.Count;
			while (m < count4)
			{
				if (!this.guiModules[m].IsDownloading && !this.guiModules[m].IsInvalid && this.guiModules[m].Module != null && this.guiModules[m].Module.Type == RuntimeModuleType.Extension)
				{
					list.Add(this.guiModules[m]);
				}
				m++;
			}
		}
		this.guiModules = list;
	}

	private void SteamUGCService_SteamUGCRemoteStoragePublishedFileSubscribed(object sender, SteamUGCRemoteStoragePublishedFileSubscribedEventArgs e)
	{
		if (e.Message.m_nAppID == (uint)global::Application.SteamAppID)
		{
			this.RefreshContent();
		}
	}

	public Transform ModuleLinePrefab;

	public AgeTransform GuiModulesTable;

	public AgeControlScrollView GuiModulesScrollView;

	public AgePrimitiveLabel InfosLabel;

	public AgeTransform NoModulesLabel;

	public AgeControlButton ValidateButton;

	private List<GuiModule> guiModules = new List<GuiModule>();

	private List<RuntimeModule> availableRuntimeModules = new List<RuntimeModule>();

	private AgeTransform.RefreshTableItem<GuiModule> setupGuiModuleDelegate;
}
