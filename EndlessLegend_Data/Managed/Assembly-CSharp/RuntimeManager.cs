using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using Amplitude;
using Amplitude.Extensions;
using Amplitude.Interop;
using Amplitude.Unity;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Runtime;
using Amplitude.Unity.Serialization;
using Amplitude.Unity.Steam;
using UnityEngine;

public class RuntimeManager : Amplitude.Unity.Runtime.RuntimeManager, IService, IRuntimeModulePlaylistService, IRuntimeModulePublicationService, IRuntimeModuleSubscriptionService
{
	public RuntimeManager()
	{
		base.RuntimeClass = typeof(global::Runtime);
		this.RuntimeChange += this.RuntimeManager_RuntimeChange;
	}

	public event EventHandler<RuntimeModulePublicationProgressEventArgs> RuntimeModulePublicationProgress;

	public event EventHandler<RuntimeModulePublicationSubmitedEventArgs> RuntimeModulePublicationSubmited;

	public event EventHandler<RuntimeModulePublicationSubmitionFailedEventArgs> RuntimeModulePublicationSubmitionFailed;

	public event EventHandler<ItemDownloadProgressEventArgs> ItemDownloadProgress;

	IRuntimeModulePublicationTask IRuntimeModulePublicationService.CurrentRuntimeModulePublicationTask
	{
		get
		{
			return this.CurrentRuntimeModulePublicationTask;
		}
	}

	bool IRuntimeModulePublicationService.Publish(RuntimeModuleEx runtimeModule, string changeNotes, RuntimeModulePublicationEventHandler callback)
	{
		if (runtimeModule == null)
		{
			Diagnostics.LogWarning("Runtime module is null.");
			return false;
		}
		if (runtimeModule.FolderName != global::RuntimeManager.Folders.UGC.Name)
		{
			Diagnostics.LogWarning("Runtime module is not publishable; it should be in folder '{0}' instead of '{1}'.", new object[]
			{
				global::RuntimeManager.Folders.UGC.Name,
				runtimeModule.FolderName
			});
			return false;
		}
		global::RuntimeManager.RuntimeModulePublicationTask runtimeModulePublicationTask = new global::RuntimeManager.RuntimeModulePublicationTask(runtimeModule, changeNotes);
		this.Publish(runtimeModulePublicationTask);
		return true;
	}

	RuntimeModuleConfigurationState IRuntimeModuleSubscriptionService.GetRuntimeModuleListState(IEnumerable<string> runtimeModuleIds, out string[] missingWorkshopItems, out string[] missingNonWorkshopItems)
	{
		string[] array;
		missingNonWorkshopItems = (array = new string[0]);
		missingWorkshopItems = array;
		if (base.Runtime == null)
		{
			return RuntimeModuleConfigurationState.Red;
		}
		if (runtimeModuleIds == null)
		{
			return RuntimeModuleConfigurationState.Red;
		}
		List<string> list = (from runtimeModuleIterator in base.Runtime.RuntimeModules
		select runtimeModuleIterator.Name.ToString()).ToList<string>();
		List<string> list2 = list.Except(runtimeModuleIds).ToList<string>();
		List<string> list3 = runtimeModuleIds.Except(list).ToList<string>();
		if (list2.Count == 0 && list3.Count == 0)
		{
			List<string> list4 = runtimeModuleIds.ToList<string>();
			for (int i = 0; i < list4.Count; i++)
			{
				if (list4[i] != list[i])
				{
					return RuntimeModuleConfigurationState.Yellow;
				}
			}
			return RuntimeModuleConfigurationState.Green;
		}
		if (list3.Count == 0)
		{
			return RuntimeModuleConfigurationState.Yellow;
		}
		List<string> list5 = new List<string>();
		List<string> list6 = new List<string>();
		foreach (string text in list3)
		{
			RuntimeModule runtimeModule;
			if (!this.RuntimeModuleDatabase.TryGetValue(text, out runtimeModule))
			{
				if (text.StartsWith(global::RuntimeManager.Folders.Workshop.Affix))
				{
					list5.Add(text);
				}
				else
				{
					list6.Add(text);
				}
			}
		}
		missingWorkshopItems = list5.ToArray();
		missingNonWorkshopItems = list6.ToArray();
		if (list6.Count != 0)
		{
			return RuntimeModuleConfigurationState.Red;
		}
		return RuntimeModuleConfigurationState.Yellow;
	}

	bool IRuntimeModuleSubscriptionService.IsInstalled(ulong publishedFileId)
	{
		if (this.SteamUGCService != null && publishedFileId != 0UL)
		{
			Steamworks.SteamUGC.EItemState itemState = (Steamworks.SteamUGC.EItemState)this.SteamUGCService.SteamUGC.GetItemState(publishedFileId);
			if ((itemState & Steamworks.SteamUGC.EItemState.k_EItemStateInstalled) == Steamworks.SteamUGC.EItemState.k_EItemStateInstalled)
			{
				return true;
			}
		}
		return false;
	}

	bool IRuntimeModuleSubscriptionService.IsSubscribed(ulong publishedFileId)
	{
		if (this.SteamUGCService != null && publishedFileId != 0UL)
		{
			Steamworks.SteamUGC.EItemState itemState = (Steamworks.SteamUGC.EItemState)this.SteamUGCService.SteamUGC.GetItemState(publishedFileId);
			if ((itemState & Steamworks.SteamUGC.EItemState.k_EItemStateSubscribed) == Steamworks.SteamUGC.EItemState.k_EItemStateSubscribed)
			{
				return true;
			}
		}
		return false;
	}

	bool IRuntimeModuleSubscriptionService.IsDownloading(ulong publishedFileId)
	{
		if (this.SteamUGCService != null && publishedFileId != 0UL)
		{
			Steamworks.SteamUGC.EItemState itemState = (Steamworks.SteamUGC.EItemState)this.SteamUGCService.SteamUGC.GetItemState(publishedFileId);
			if ((itemState & Steamworks.SteamUGC.EItemState.k_EItemStateDownloading) == Steamworks.SteamUGC.EItemState.k_EItemStateDownloading)
			{
				return true;
			}
		}
		return false;
	}

	void IRuntimeModuleSubscriptionService.Subscribe(ulong publishedFileId)
	{
		if (publishedFileId != 0UL && this.SteamUGCService != null && this.SteamUGCService.SteamUGC != null)
		{
			this.SteamUGCService.SteamUGC.SubscribeItem(publishedFileId);
		}
	}

	void IRuntimeModuleSubscriptionService.Unsubscribe(ulong publishedFileId)
	{
		if (publishedFileId != 0UL && this.SteamUGCService != null && this.SteamUGCService.SteamUGC != null)
		{
			this.SteamUGCService.SteamUGC.UnsubscribeItem(publishedFileId);
		}
	}

	public ModulePlaylist CurrentModulePlaylist { get; set; }

	private global::RuntimeManager.RuntimeModulePublicationTask CurrentRuntimeModulePublicationTask { get; set; }

	private void DeleteSteamWorkshopCompanionFile(RuntimeModuleEx runtimeModule)
	{
		try
		{
			string path = System.IO.Path.Combine(runtimeModule.DirectoryName, "PublishedFile.Id");
			if (File.Exists(path))
			{
				File.Delete(path);
			}
			runtimeModule.PublishedFileId = 0UL;
		}
		catch
		{
		}
	}

	private void ExportSteamWorkshopCompanionFile(RuntimeModuleEx runtimeModule)
	{
		try
		{
			string text = System.IO.Path.Combine(runtimeModule.DirectoryName, "PublishedFile.Id");
			if (File.Exists(text))
			{
				string text2 = File.ReadAllText(text);
				ulong num = ulong.Parse(text2);
				if (num == runtimeModule.PublishedFileId)
				{
					return;
				}
				Diagnostics.LogWarning("Published file id mismatch in file '{0}' ({1}) for runtime module '{2}' with {3}.", new object[]
				{
					text,
					text2,
					runtimeModule.Name,
					runtimeModule.PublishedFileId
				});
			}
			File.WriteAllText(text, runtimeModule.PublishedFileId.ToString());
		}
		catch
		{
		}
	}

	private ulong ImportSteamWorkshopCompanionFile(RuntimeModuleEx runtimeModule)
	{
		try
		{
			string text = System.IO.Path.Combine(runtimeModule.DirectoryName, "PublishedFile.Id");
			if (File.Exists(text))
			{
				string text2 = File.ReadAllText(text);
				ulong num = ulong.Parse(text2);
				if (runtimeModule.PublishedFileId != 0UL && runtimeModule.PublishedFileId != num)
				{
					Diagnostics.LogWarning("Published file id mismatch in file '{0}' ({1}) for runtime module '{2}' with {3}.", new object[]
					{
						text,
						text2,
						runtimeModule.Name,
						runtimeModule.PublishedFileId
					});
				}
				runtimeModule.PublishedFileId = num;
				return num;
			}
		}
		catch
		{
		}
		return 0UL;
	}

	private void OnRuntimeModulePublicationProgress(RuntimeModulePublicationProgressEventArgs e)
	{
		if (this.RuntimeModulePublicationProgress != null)
		{
			this.RuntimeModulePublicationProgress(this, e);
		}
	}

	private void OnRuntimeModulePublicationSubmited(RuntimeModulePublicationSubmitedEventArgs e)
	{
		if (this.RuntimeModulePublicationSubmited != null)
		{
			this.RuntimeModulePublicationSubmited(this, e);
		}
	}

	private void OnRuntimeModulePublicationSubmitionFailed(RuntimeModulePublicationSubmitionFailedEventArgs e)
	{
		if (this.RuntimeModulePublicationSubmitionFailed != null)
		{
			this.RuntimeModulePublicationSubmitionFailed(this, e);
		}
	}

	private void Publish(global::RuntimeManager.RuntimeModulePublicationTask runtimeModulePublicationTask)
	{
		Diagnostics.Assert(runtimeModulePublicationTask != null);
		Diagnostics.Assert(runtimeModulePublicationTask.RuntimeModule != null);
		if (this.CurrentRuntimeModulePublicationTask != null)
		{
			Diagnostics.Log("Postponing the publication of runtime module '{0}' because another one is already in progress.", new object[]
			{
				runtimeModulePublicationTask.RuntimeModule.Name
			});
			this.runtimeModulePublicationTasks.Enqueue(runtimeModulePublicationTask);
			return;
		}
		if (runtimeModulePublicationTask.RuntimeModule.PublishedFileId != 0UL)
		{
			this.CurrentRuntimeModulePublicationTask = runtimeModulePublicationTask;
			this.SubmitItemUpdate();
			return;
		}
		uint steamAppID = (uint)global::Application.SteamAppID;
		ulong num = this.SteamUGCService.CreateItem(steamAppID, Steamworks.SteamRemoteStorage.EWorkshopFileType.k_EWorkshopFileTypeFirst);
		if (num != 0UL)
		{
			this.CurrentRuntimeModulePublicationTask = runtimeModulePublicationTask;
		}
		else if (this.runtimeModulePublicationTasks.Count != 0)
		{
			runtimeModulePublicationTask = this.runtimeModulePublicationTasks.Dequeue();
			this.Publish(runtimeModulePublicationTask);
		}
	}

	private void SteamUGCService_SteamUGCItemCreated(object sender, SteamUGCCreateItemEventArgs e)
	{
		if (this.CurrentRuntimeModulePublicationTask == null)
		{
			Diagnostics.LogWarning("SteamUGCService_SteamUGCItemCreated! But there is no publication task in progress...");
			return;
		}
		if (e.Message.m_eResult != Steamworks.EResult.k_EResultOK)
		{
			Diagnostics.LogWarning("SteamUGCService_SteamUGCItemCreated! Item creation has failed with result: {0}.", new object[]
			{
				e.Message.m_eResult
			});
			return;
		}
		if (this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId != e.Message.m_nPublishedFileId)
		{
			if (this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId != 0UL)
			{
				Diagnostics.LogWarning("SteamUGCService_SteamUGCItemCreated! 'PublishedFileId' mismatch: received {0} while current task's associated one is {1}.", new object[]
				{
					e.Message.m_nPublishedFileId,
					this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId
				});
				return;
			}
			this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId = e.Message.m_nPublishedFileId;
		}
		this.SubmitItemUpdate();
	}

	private void SteamUGCService_SteamUGCItemUpdateSubmited(object sender, SteamUGCSubmitItemUpdateEventArgs e)
	{
		base.StopCoroutine("TrackSubmitItemUpdateProgress");
		if (e.Message.m_eResult == Steamworks.EResult.k_EResultOK)
		{
			if (this.CurrentRuntimeModulePublicationTask != null)
			{
				this.ExportSteamWorkshopCompanionFile(this.CurrentRuntimeModulePublicationTask.RuntimeModule);
				this.OnRuntimeModulePublicationSubmited(new RuntimeModulePublicationSubmitedEventArgs(this.CurrentRuntimeModulePublicationTask));
				if (this.CurrentRuntimeModulePublicationTask.RuntimeModule != null)
				{
					this.SteamUGCService.SteamUGC.SubscribeItem(this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId);
					if (this.CurrentRuntimeModulePublicationTask.PublishedFileId == this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId)
					{
						bool flag = this.SteamUGCService.SteamUGC.DownloadItem(this.CurrentRuntimeModulePublicationTask.PublishedFileId, true);
						if (flag)
						{
							Diagnostics.Log("Item update submited, download initiated (PublishedFileId: {0})...", new object[]
							{
								this.CurrentRuntimeModulePublicationTask.PublishedFileId
							});
							this.pendingDownloads.AddOnce(this.CurrentRuntimeModulePublicationTask.PublishedFileId);
							this.TrackItemDownloadProgress(this.CurrentRuntimeModulePublicationTask.PublishedFileId);
						}
					}
				}
			}
		}
		else if (this.CurrentRuntimeModulePublicationTask != null)
		{
			Diagnostics.Log("Item update submition has failed (PublishedFileId: {0}).", new object[]
			{
				this.CurrentRuntimeModulePublicationTask.PublishedFileId
			});
			if (e.Message.m_eResult == Steamworks.EResult.k_EResultFileNotFound || e.Message.m_eResult == Steamworks.EResult.k_EResultFail)
			{
				this.DeleteSteamWorkshopCompanionFile(this.CurrentRuntimeModulePublicationTask.RuntimeModule);
			}
			this.OnRuntimeModulePublicationSubmitionFailed(new RuntimeModulePublicationSubmitionFailedEventArgs(e.Message.m_eResult, this.CurrentRuntimeModulePublicationTask));
		}
		this.CurrentRuntimeModulePublicationTask = null;
		if (this.runtimeModulePublicationTasks.Count != 0)
		{
			global::RuntimeManager.RuntimeModulePublicationTask runtimeModulePublicationTask = this.runtimeModulePublicationTasks.Dequeue();
			this.Publish(runtimeModulePublicationTask);
		}
	}

	private void SubmitItemUpdate()
	{
		Diagnostics.Assert(this.CurrentRuntimeModulePublicationTask != null);
		Diagnostics.Assert(this.CurrentRuntimeModulePublicationTask.RuntimeModule != null);
		Diagnostics.Assert(this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId != 0UL);
		uint steamAppID = (uint)global::Application.SteamAppID;
		ulong num = this.SteamUGCService.SteamUGC.StartItemUpdate(steamAppID, this.CurrentRuntimeModulePublicationTask.RuntimeModule.PublishedFileId);
		if (num != 0UL)
		{
			this.CurrentRuntimeModulePublicationTask.UpdateHandle = num;
			if (string.IsNullOrEmpty(this.CurrentRuntimeModulePublicationTask.RuntimeModule.Title))
			{
				Diagnostics.LogWarning("Runtime module title is null or empty.");
			}
			else if (this.CurrentRuntimeModulePublicationTask.RuntimeModule.Title.Length >= 129)
			{
				Diagnostics.LogWarning("Runtime module title is too long ({0} characters max.).", new object[]
				{
					129
				});
			}
			else
			{
				this.SteamUGCService.SteamUGC.SetItemTitle(num, this.CurrentRuntimeModulePublicationTask.RuntimeModule.Title);
			}
			if (!string.IsNullOrEmpty(this.CurrentRuntimeModulePublicationTask.RuntimeModule.Description))
			{
				if (this.CurrentRuntimeModulePublicationTask.RuntimeModule.Description.Length >= 8000)
				{
					Diagnostics.LogWarning("Runtime module description is too long ({0} characters max.).", new object[]
					{
						8000
					});
				}
				else
				{
					this.SteamUGCService.SteamUGC.SetItemDescription(num, this.CurrentRuntimeModulePublicationTask.RuntimeModule.Description);
				}
			}
			this.SteamUGCService.SteamUGC.SetItemVisibility(num, Steamworks.SteamRemoteStorage.ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
			if (this.CurrentRuntimeModulePublicationTask.RuntimeModule.Tags != null)
			{
				List<string> list = new List<string>();
				string[] names = Enum.GetNames(typeof(RuntimeModuleTags));
				for (int i = 0; i < this.CurrentRuntimeModulePublicationTask.RuntimeModule.Tags.Length; i++)
				{
					string text = this.CurrentRuntimeModulePublicationTask.RuntimeModule.Tags[i];
					if (names.Contains(text))
					{
						RuntimeModuleTags runtimeModuleTags = (RuntimeModuleTags)((int)Enum.Parse(typeof(RuntimeModuleTags), text));
						if (runtimeModuleTags < RuntimeModuleTags.NonPublishable)
						{
							list.AddOnce(text);
						}
					}
				}
				if (list.Count != 0)
				{
					this.SteamUGCService.SteamUGC.SetItemTags(num, list);
				}
			}
			string path = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.GameDirectory, this.CurrentRuntimeModulePublicationTask.RuntimeModule.FolderName);
			string text2 = System.IO.Path.Combine(path, this.CurrentRuntimeModulePublicationTask.RuntimeModule.DirectoryName);
			if (!string.IsNullOrEmpty(this.CurrentRuntimeModulePublicationTask.RuntimeModule.PreviewImageFile))
			{
				string text3 = System.IO.Path.Combine(text2, this.CurrentRuntimeModulePublicationTask.RuntimeModule.PreviewImageFile);
				if (File.Exists(text3))
				{
					this.SteamUGCService.SteamUGC.SetItemPreview(num, text3);
				}
			}
			this.SteamUGCService.SteamUGC.SetItemContent(num, text2);
			ulong num2 = this.SteamUGCService.SubmitItemUpdate(num, this.CurrentRuntimeModulePublicationTask.ChangeNotes);
			if (num2 != 0UL)
			{
				Diagnostics.Log("SubmitItemUpdate() in progress...");
				base.StartCoroutine("TrackSubmitItemUpdateProgress", num);
			}
		}
	}

	private IEnumerator TrackSubmitItemUpdateProgress(ulong updateHandle)
	{
		Diagnostics.Assert(this.CurrentRuntimeModulePublicationTask != null);
		Diagnostics.Assert(this.CurrentRuntimeModulePublicationTask.UpdateHandle == updateHandle);
		while (this.CurrentRuntimeModulePublicationTask != null && this.CurrentRuntimeModulePublicationTask.UpdateHandle != 0UL)
		{
			ulong bytesProcessed = 0UL;
			ulong bytesTotal = 0UL;
			Steamworks.SteamUGC.EItemUpdateStatus status = this.SteamUGCService.SteamUGC.GetItemUpdateProgress(this.CurrentRuntimeModulePublicationTask.UpdateHandle, out bytesProcessed, out bytesTotal);
			if (bytesProcessed != 0UL || bytesTotal != 0UL)
			{
				this.CurrentRuntimeModulePublicationTask.BytesProcessed = bytesProcessed;
				this.CurrentRuntimeModulePublicationTask.BytesTotal = bytesTotal;
			}
			else
			{
				this.CurrentRuntimeModulePublicationTask.BytesProcessed = 0UL;
				this.CurrentRuntimeModulePublicationTask.BytesTotal = 0UL;
			}
			this.CurrentRuntimeModulePublicationTask.ItemUpdateStatus = status;
			this.OnRuntimeModulePublicationProgress(new RuntimeModulePublicationProgressEventArgs(this.CurrentRuntimeModulePublicationTask));
			yield return new WaitForSeconds(0.2f);
		}
		yield break;
	}

	public ReadOnlyCollection<ulong> Downloads
	{
		get
		{
			return this.pendingDownloads.AsReadOnly();
		}
	}

	private void SteamUGCService_SteamUGCDownloadItemResult(object sender, SteamUGCDownloadItemResultEventArgs e)
	{
		Steamworks.EResult eResult = e.Message.m_eResult;
		if (eResult == Steamworks.EResult.k_EResultOK)
		{
			base.SetLastError(0, "Loading the runtime module from the steam workshop folder (steam AppId: {0}, PublishedFileId: {1})...", new object[]
			{
				e.Message.m_nAppID,
				e.Message.m_nPublishedFileId
			});
			try
			{
				if (this.RuntimeModuleSerializer == null)
				{
					ISerializationService service = Services.GetService<ISerializationService>();
					if (service != null)
					{
						this.RuntimeModuleSerializer = service.GetXmlSerializer<RuntimeModuleEx>();
					}
					if (this.RuntimeModuleSerializer == null)
					{
						Diagnostics.LogWarning("Failed to instanciate an xml runtime module serializer.");
					}
				}
				ulong[] subscribedItems = new ulong[]
				{
					e.Message.m_nPublishedFileId
				};
				Amplitude.Coroutine coroutine = Amplitude.Coroutine.StartCoroutine(this.LoadDatabasesFromSteamWorkshopFiles(subscribedItems, 1u, true), null);
				coroutine.RunUntilIsFinished();
			}
			catch
			{
			}
			finally
			{
				this.RuntimeModuleSerializer = null;
			}
		}
		if (e.Message.m_nAppID == (uint)global::Application.SteamAppID)
		{
			this.pendingDownloads.Remove(e.Message.m_nPublishedFileId);
		}
	}

	private void SteamUGCService_SteamUGCRemoteStoragePublishedFileSubscribed(object sender, SteamUGCRemoteStoragePublishedFileSubscribedEventArgs e)
	{
		Diagnostics.Log("RemoteStoragePublishedFileSubscribed(steam AppId: {0}, PublishedFileId: {1})!", new object[]
		{
			e.Message.m_nAppID,
			e.Message.m_nPublishedFileId
		});
		if (e.Message.m_nAppID == (uint)global::Application.SteamAppID && e.Message.m_nPublishedFileId != 0UL)
		{
			bool flag = this.SteamUGCService.SteamUGC.DownloadItem(e.Message.m_nPublishedFileId, false);
			if (flag)
			{
				Diagnostics.Log("Item download initiated (steam AppId: {0}, PublishedFileId: {1})...", new object[]
				{
					e.Message.m_nAppID,
					e.Message.m_nPublishedFileId
				});
				this.pendingDownloads.AddOnce(e.Message.m_nPublishedFileId);
				this.TrackItemDownloadProgress(e.Message.m_nPublishedFileId);
			}
		}
	}

	private void SteamUGCService_SteamUGCRemoteStoragePublishedFileUnsubscribed(object sender, SteamUGCRemoteStoragePublishedFileUnsubscribedEventArgs e)
	{
		Diagnostics.Log("RemoteStoragePublishedFileUnsubscribed(steam AppId: {0}, PublishedFileId: {1})!", new object[]
		{
			e.Message.m_nAppID,
			e.Message.m_nPublishedFileId
		});
		if (e.Message.m_nAppID == (uint)global::Application.SteamAppID)
		{
			string x = global::RuntimeManager.Folders.Workshop.Affix + e.Message.m_nPublishedFileId.ToString();
			RuntimeModule runtimeModule;
			if (this.RuntimeModuleDatabase.TryGetValue(x, out runtimeModule))
			{
				this.RuntimeModuleDatabase.Remove(runtimeModule);
				this.OnRuntimeModuleDatabaseUpdate(new RuntimeModuleDatabaseUpdateEventArgs(RuntimeModuleDatabaseUpdateAction.Updated, runtimeModule));
			}
		}
	}

	private void TrackItemDownloadProgress(ulong publishedFileId)
	{
		if (this.pendingDownloads == null || this.pendingDownloads.Count == 0)
		{
			return;
		}
		if (this.downloadProgressAsync == null)
		{
			this.downloadProgressAsync = UnityCoroutine.StartCoroutine(this, this.TrackItemDownloadProgressAsync(), null);
		}
	}

	private IEnumerator TrackItemDownloadProgressAsync()
	{
		while (this.pendingDownloads.Count != 0)
		{
			yield return Amplitude.Coroutine.WaitForSeconds(0.5f);
			if (this.ItemDownloadProgress != null)
			{
				ulong[] publishedFileIds = this.pendingDownloads.ToArray();
				for (int index = 0; index < publishedFileIds.Length; index++)
				{
					ulong bytesDownloaded;
					ulong bytesTotal;
					if (this.SteamUGCService.SteamUGC.GetItemDownloadInfo(publishedFileIds[index], out bytesDownloaded, out bytesTotal) && bytesTotal != 0UL && this.ItemDownloadProgress != null)
					{
						this.ItemDownloadProgress(this, new ItemDownloadProgressEventArgs(publishedFileIds[index], bytesDownloaded, bytesTotal));
					}
				}
			}
		}
		this.downloadProgressAsync = null;
		yield break;
	}

	private ISteamUGCService SteamUGCService { get; set; }

	public override IEnumerator BindServices()
	{
		base.SetLastError(0, "Waiting for service dependencies...");
		yield return base.BindService<ISteamUGCService>(delegate(ISteamUGCService service)
		{
			this.SteamUGCService = service;
		});
		this.SteamUGCService.SteamUGCRemoteStoragePublishedFileSubscribed += this.SteamUGCService_SteamUGCRemoteStoragePublishedFileSubscribed;
		this.SteamUGCService.SteamUGCRemoteStoragePublishedFileUnsubscribed += this.SteamUGCService_SteamUGCRemoteStoragePublishedFileUnsubscribed;
		this.SteamUGCService.SteamUGCQueryCompleted += this.SteamUGCService_SteamUGCQueryCompleted;
		this.SteamUGCService.SteamUGCItemCreated += this.SteamUGCService_SteamUGCItemCreated;
		this.SteamUGCService.SteamUGCItemUpdateSubmited += this.SteamUGCService_SteamUGCItemUpdateSubmited;
		this.SteamUGCService.SteamUGCDownloadItemResult += this.SteamUGCService_SteamUGCDownloadItemResult;
		Services.AddService<IRuntimeModulePublicationService>(this);
		Services.AddService<IRuntimeModuleSubscriptionService>(this);
		Services.AddService<IRuntimeModulePlaylistService>(this);
		yield return base.BindServices();
		yield break;
	}

	private IEnumerator LoadDatabasesFromSteamWorkshopFiles(ulong[] subscribedItems, uint numberOfSubscribedItems, bool replaceExistingRuntimeModules = false)
	{
		if (this.steamAPICall_RequestUGCDetails == 0UL)
		{
			int index = 0;
			while ((long)index < (long)((ulong)numberOfSubscribedItems))
			{
				ulong installSizeOnDisk;
				string installFolder;
				uint timeStamp;
				this.SteamUGCService.SteamUGC.GetItemInstallInfo(subscribedItems[index], out installSizeOnDisk, out installFolder, out timeStamp);
				if (!string.IsNullOrEmpty(installFolder))
				{
					DirectoryInfo directoryInfo = new DirectoryInfo(installFolder);
					if (directoryInfo.Exists)
					{
						FileInfo[] files = directoryInfo.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
						if (files != null && files.Length > 0)
						{
							FileInfo[] array = files;
							int i = 0;
							while (i < array.Length)
							{
								FileInfo file = array[i];
								try
								{
									RuntimeModuleEx runtimeModule = this.LoadModule(file);
									if (runtimeModule != null)
									{
										if (string.IsNullOrEmpty(runtimeModule.Name))
										{
											Diagnostics.LogWarning("Invalid runtime module definition file '{0}', name is either null or empty.", new object[]
											{
												file.Name
											});
										}
										else
										{
											string prefixedRuntimeModuleName = global::RuntimeManager.Folders.Workshop.Affix + subscribedItems[index].ToString();
											bool runtimeModuleAlreadyRegistered = this.RuntimeModuleDatabase.ContainsKey(prefixedRuntimeModuleName);
											if (runtimeModuleAlreadyRegistered)
											{
												if (!replaceExistingRuntimeModules)
												{
													Diagnostics.LogWarning("Ignoring runtime module definition file '{0}', another runtime module with the same name '{1}' has already been registered.", new object[]
													{
														file.Name,
														prefixedRuntimeModuleName
													});
													goto IL_3A3;
												}
												Diagnostics.LogWarning("Updating existing runtime module (name '{0}') which has already been registered.", new object[]
												{
													prefixedRuntimeModuleName
												});
												this.RuntimeModuleDatabase.Remove(prefixedRuntimeModuleName);
											}
											runtimeModule.Name = prefixedRuntimeModuleName;
											runtimeModule.DirectoryName = directoryInfo.FullName;
											runtimeModule.FolderName = global::RuntimeManager.Folders.Workshop.Name;
											runtimeModule.Tags.AddTag(RuntimeModuleTags.Workshop.ToString());
											DirectoryInfo resourcesFolder = new DirectoryInfo(System.IO.Path.Combine(directoryInfo.FullName, "Resources"));
											if (resourcesFolder.Exists)
											{
												runtimeModule.ResourcesFolder = resourcesFolder;
											}
											runtimeModule.PublishedFileId = subscribedItems[index];
											this.RuntimeModuleDatabase.Add(runtimeModule);
											if (replaceExistingRuntimeModules)
											{
												if (runtimeModuleAlreadyRegistered)
												{
													this.OnRuntimeModuleDatabaseUpdate(new RuntimeModuleDatabaseUpdateEventArgs(RuntimeModuleDatabaseUpdateAction.Updated, runtimeModule));
												}
												else
												{
													this.OnRuntimeModuleDatabaseUpdate(new RuntimeModuleDatabaseUpdateEventArgs(RuntimeModuleDatabaseUpdateAction.Added, runtimeModule));
												}
											}
										}
									}
									else
									{
										Diagnostics.Log("Discarding invalid runtime module definition file '{0}'.", new object[]
										{
											file.Name
										});
									}
								}
								catch (Exception ex)
								{
									Exception exception = ex;
									Diagnostics.LogWarning("Exception caught: {0}", new object[]
									{
										exception.Message
									});
								}
								goto IL_390;
								IL_3A3:
								i++;
								continue;
								IL_390:
								yield return null;
								goto IL_3A3;
							}
						}
					}
				}
				index++;
			}
		}
		while (this.steamAPICall_RequestUGCDetails != 0UL)
		{
			yield return null;
		}
		yield break;
	}

	private IEnumerator LoadDatabasesFromSteamWorkshopFiles()
	{
		base.SetLastError(0, "Loading the module databases from steam workshop folder...");
		if (this.steamAPICall_RequestUGCDetails == 0UL)
		{
			ulong[] subscribedItems = new ulong[64];
			uint numberOfSubscribedItems = this.SteamUGCService.SteamUGC.GetSubscribedItems(ref subscribedItems);
			if ((ulong)numberOfSubscribedItems > (ulong)((long)subscribedItems.Length))
			{
				subscribedItems = new ulong[numberOfSubscribedItems];
				numberOfSubscribedItems = this.SteamUGCService.SteamUGC.GetSubscribedItems(ref subscribedItems);
			}
			if (numberOfSubscribedItems > 0u)
			{
				yield return this.LoadDatabasesFromSteamWorkshopFiles(subscribedItems, numberOfSubscribedItems, false);
			}
		}
		yield break;
	}

	private void SteamUGCService_SteamUGCQueryCompleted(object sender, SteamUGCQueryCompletedEventArgs e)
	{
		this.steamAPICall_RequestUGCDetails = 0UL;
		if (e.Message.m_eResult == Steamworks.EResult.k_EResultOK)
		{
			for (uint num = 0u; num < e.Message.m_unNumResultsReturned; num += 1u)
			{
				Steamworks.SteamUGC.SteamUGCDetails steamUGCDetails = default(Steamworks.SteamUGC.SteamUGCDetails);
				bool queryUGCResult = this.SteamUGCService.SteamUGC.GetQueryUGCResult(e.Message.m_handle, num, ref steamUGCDetails);
				if (queryUGCResult)
				{
				}
			}
		}
	}

	public static object LobbyData { get; set; }

	public IDatabase<RuntimeModule> RuntimeModuleDatabase { get; private set; }

	public int LastRevision { get; private set; }

	private XmlSerializer RuntimeModuleSerializer { get; set; }

	protected override IEnumerator LoadDatabases()
	{
		if (this.RuntimeModuleDatabase == null)
		{
			this.RuntimeModuleDatabase = Databases.GetDatabase<RuntimeModule>(true);
		}
		this.RuntimeModuleDatabase.Clear();
		yield return base.LoadDatabases();
		ISerializationService serializationService = Services.GetService<ISerializationService>();
		if (serializationService != null)
		{
			this.RuntimeModuleSerializer = serializationService.GetXmlSerializer<RuntimeModuleEx>();
		}
		if (this.RuntimeModuleSerializer == null)
		{
			Diagnostics.LogWarning("Failed to instanciate an xml runtime module serializer.");
			yield break;
		}
		yield return this.LoadDatabasesFromCommunityFiles();
		if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools)
		{
			Diagnostics.Log("Modding tools enabled, loading user generated content files...");
			yield return this.LoadDatabasesFromUserGeneratedContentFiles();
		}
		yield break;
	}

	private IEnumerator LoadDatabases(global::RuntimeManager.Folders.FolderInfo folderInfo)
	{
		base.SetLastError(0, "Loading the module databases from folder '{0}'...", new object[]
		{
			folderInfo.Name
		});
		string path = System.IO.Path.Combine(Amplitude.Unity.Framework.Application.GameDirectory, folderInfo.Name);
		DirectoryInfo directoryInfo = new DirectoryInfo(path);
		if (directoryInfo.Exists)
		{
			DirectoryInfo[] subFolders = directoryInfo.GetDirectories();
			foreach (DirectoryInfo subFolder in subFolders)
			{
				FileInfo[] files = subFolder.GetFiles("*.xml", SearchOption.TopDirectoryOnly);
				if (files != null && files.Length > 0)
				{
					foreach (FileInfo file in files)
					{
						try
						{
							RuntimeModuleEx runtimeModule = this.LoadModule(file);
							if (runtimeModule != null)
							{
								if (string.IsNullOrEmpty(runtimeModule.Name))
								{
									Diagnostics.LogWarning("Invalid runtime module definition file '{0}', name is either null or empty.", new object[]
									{
										file.Name
									});
								}
								else
								{
									string prefixedRuntimeModuleName = folderInfo.Affix + runtimeModule.Name;
									if (this.RuntimeModuleDatabase.ContainsKey(prefixedRuntimeModuleName))
									{
										Diagnostics.LogWarning("Ignoring runtime module definition file '{0}', another runtime module with the same name '{1}' has already been registered.", new object[]
										{
											file.Name,
											runtimeModule.Name
										});
									}
									else
									{
										runtimeModule.Name = prefixedRuntimeModuleName;
										runtimeModule.DirectoryName = subFolder.FullName;
										runtimeModule.FolderName = folderInfo.Name;
										if (folderInfo.Name == global::RuntimeManager.Folders.Community.Name)
										{
											runtimeModule.Tags.AddTag(RuntimeModuleTags.Community.ToString());
										}
										if (folderInfo.Name == global::RuntimeManager.Folders.UGC.Name)
										{
											runtimeModule.Tags.AddTag(RuntimeModuleTags.UGC.ToString());
											this.ImportSteamWorkshopCompanionFile(runtimeModule);
										}
										DirectoryInfo resourcesFolder = new DirectoryInfo(System.IO.Path.Combine(subFolder.FullName, "Resources"));
										if (resourcesFolder.Exists)
										{
											runtimeModule.ResourcesFolder = resourcesFolder;
										}
										this.RuntimeModuleDatabase.Add(runtimeModule);
									}
								}
							}
							else
							{
								Diagnostics.Log("Discarding invalid runtime module definition file '{0}'.", new object[]
								{
									file.Name
								});
							}
						}
						catch (Exception ex)
						{
							Exception exception = ex;
							Diagnostics.LogWarning("Exception caught: {0}", new object[]
							{
								exception.Message
							});
						}
						yield return null;
					}
				}
			}
		}
		yield break;
	}

	private IEnumerator LoadDatabasesFromCommunityFiles()
	{
		yield return this.LoadDatabases(global::RuntimeManager.Folders.Community);
		yield break;
	}

	private IEnumerator LoadDatabasesFromUserGeneratedContentFiles()
	{
		yield return this.LoadDatabases(global::RuntimeManager.Folders.UGC);
		yield break;
	}

	private RuntimeModuleEx LoadModule(FileInfo file)
	{
		if (file == null)
		{
			Diagnostics.LogWarning("File info is null.");
			return null;
		}
		if (!file.Exists)
		{
			Diagnostics.LogWarning("File does not exist (FullName: '{0}').", new object[]
			{
				file.FullName
			});
			return null;
		}
		if (this.RuntimeModuleSerializer == null)
		{
			Diagnostics.LogWarning("Runtime module serializer is null.");
			return null;
		}
		try
		{
			XmlReaderSettings settings = new XmlReaderSettings
			{
				CloseInput = true,
				IgnoreComments = true,
				IgnoreProcessingInstructions = true,
				IgnoreWhitespace = true
			};
			using (XmlReader xmlReader = XmlReader.Create(file.OpenRead(), settings))
			{
				if (xmlReader.ReadToFollowing("Datatable") && xmlReader.Depth == 0 && xmlReader.ReadToDescendant("RuntimeModule"))
				{
					string text = xmlReader.GetAttribute("Name");
					string attribute = xmlReader.GetAttribute("Type");
					if (string.IsNullOrEmpty(text))
					{
						Diagnostics.LogWarning("Invalid runtime module definition file '{0}', name is either null or empty.", new object[]
						{
							file.Name
						});
					}
					else if (text.IndexOfAny(Amplitude.String.Separators) >= 0)
					{
						Diagnostics.LogWarning("Invalid runtime module definition file '{0}', name '{1}' must not contain characters ',' or ';'.", new object[]
						{
							file.Name,
							text
						});
						text = null;
					}
					if (!string.IsNullOrEmpty(text) && Enum.GetNames(typeof(RuntimeModuleType)).Contains(attribute))
					{
						Diagnostics.Log("Loading runtime module definition from file '{0}'...", new object[]
						{
							file.Name
						});
						RuntimeModuleEx runtimeModuleEx = this.RuntimeModuleSerializer.Deserialize(xmlReader) as RuntimeModuleEx;
						if (runtimeModuleEx != null)
						{
							string text2;
							string text3;
							if (!Amplitude.Unity.Runtime.Runtime.CheckRuntimeModuleName(runtimeModuleEx.Name, out text2, out text3))
							{
								Diagnostics.LogWarning("Invalid runtime module name '{0}', discarding the module.", new object[]
								{
									runtimeModuleEx.Name
								});
								return null;
							}
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.Conversion.ToString());
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.Extension.ToString());
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.Standalone.ToString());
							switch (runtimeModuleEx.Type)
							{
							case RuntimeModuleType.Standalone:
								runtimeModuleEx.Tags.AddTag(RuntimeModuleTags.Standalone.ToString());
								break;
							case RuntimeModuleType.Conversion:
								runtimeModuleEx.Tags.AddTag(RuntimeModuleTags.Conversion.ToString());
								break;
							case RuntimeModuleType.Extension:
								runtimeModuleEx.Tags.AddTag(RuntimeModuleTags.Extension.ToString());
								break;
							}
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.Community.ToString());
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.UGC.ToString());
							runtimeModuleEx.Tags.RemoveTag(RuntimeModuleTags.Workshop.ToString());
							return runtimeModuleEx;
						}
					}
					else
					{
						Diagnostics.Log("Discarding invalid runtime module definition file '{0}'.", new object[]
						{
							file.Name
						});
					}
				}
			}
		}
		catch (Exception ex)
		{
			Diagnostics.LogWarning("Exception caught: {0}", new object[]
			{
				ex.Message
			});
		}
		return null;
	}

	private void RuntimeManager_RuntimeChange(object sender, RuntimeChangeEventArgs e)
	{
		switch (e.Action)
		{
		case RuntimeChangeAction.Loaded:
			this.RuntimeModuleSerializer = null;
			WorldGeneratorScenarioDefinition.Select(true);
			break;
		case RuntimeChangeAction.Unloaded:
			if (this.LastRevision > 0)
			{
				Databases.RollbackTo(this.LastRevision);
			}
			break;
		case RuntimeChangeAction.Loading:
			this.LastRevision = Databases.CurrentRevision;
			Databases.Commit();
			if (this.RuntimeModuleSerializer == null)
			{
				ISerializationService service = Services.GetService<ISerializationService>();
				if (service != null)
				{
					this.RuntimeModuleSerializer = service.GetXmlSerializer<RuntimeModuleEx>();
				}
			}
			if (this.RuntimeModuleSerializer != null)
			{
				Amplitude.Coroutine coroutine = Amplitude.Coroutine.StartCoroutine(this.LoadDatabasesFromSteamWorkshopFiles(), null);
				coroutine.RunUntilIsFinished();
			}
			break;
		}
	}

	private Queue<global::RuntimeManager.RuntimeModulePublicationTask> runtimeModulePublicationTasks = new Queue<global::RuntimeManager.RuntimeModulePublicationTask>();

	private List<ulong> pendingDownloads = new List<ulong>();

	private UnityCoroutine downloadProgressAsync;

	private ulong steamAPICall_RequestUGCDetails;

	private class RuntimeModulePublicationTask : IRuntimeModulePublicationTask, IRuntimeModuleTask
	{
		public RuntimeModulePublicationTask(RuntimeModuleEx runtimeModule)
		{
			this.RuntimeModule = runtimeModule;
			this.ChangeNotes = string.Empty;
			this.PublishedFileId = runtimeModule.PublishedFileId;
		}

		public RuntimeModulePublicationTask(RuntimeModuleEx runtimeModule, string changeNotes)
		{
			this.RuntimeModule = runtimeModule;
			this.ChangeNotes = changeNotes;
			this.PublishedFileId = runtimeModule.PublishedFileId;
		}

		public ulong BytesProcessed { get; internal set; }

		public ulong BytesTotal { get; internal set; }

		public string ChangeNotes { get; private set; }

		public Steamworks.SteamUGC.EItemUpdateStatus ItemUpdateStatus { get; internal set; }

		public ulong PublishedFileId { get; private set; }

		public RuntimeModuleEx RuntimeModule { get; private set; }

		internal ulong UpdateHandle { get; set; }
	}

	public struct Folders
	{
		public static readonly global::RuntimeManager.Folders.FolderInfo Community = new global::RuntimeManager.Folders.FolderInfo("Community", string.Empty);

		public static readonly global::RuntimeManager.Folders.FolderInfo UGC = new global::RuntimeManager.Folders.FolderInfo("User Generated Content", "u:/");

		public static readonly global::RuntimeManager.Folders.FolderInfo Workshop = new global::RuntimeManager.Folders.FolderInfo("Workshop", "w:/");

		public struct FolderInfo
		{
			public FolderInfo(string name, string affix)
			{
				this.Name = name;
				this.Affix = affix;
			}

			public readonly string Name;

			public readonly string Affix;
		}
	}
}
