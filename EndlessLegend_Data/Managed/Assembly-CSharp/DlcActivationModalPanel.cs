using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Amplitude;
using Amplitude.Interop;
using Amplitude.Unity.Framework;
using UnityEngine;

public class DlcActivationModalPanel : GuiModalPanel
{
	public override bool HandleCancelRequest()
	{
		if (this.modified)
		{
			MessagePanel.Instance.Show("%ConfirmUndoDLCchanges", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.DoCancelRequest), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
			return true;
		}
		return base.HandleCancelRequest();
	}

	public override void RefreshContent()
	{
		base.RefreshContent();
		this.currentY = 0f;
		this.filteredDownloadableContents.Clear();
		foreach (DownloadableContent downloadableContent in this.downloadableContents)
		{
			if ((this.ExpansionsToggle.State && downloadableContent.Type == DownloadableContentType.Exclusive) || (this.AddOnsToggle.State && downloadableContent.Type == DownloadableContentType.Personal) || (this.UpdatesToggle.State && downloadableContent.Type == DownloadableContentType.Addon))
			{
				this.filteredDownloadableContents.Add(downloadableContent);
			}
			if (!this.modified)
			{
				this.downloadableContentActivationState[downloadableContent.Name] = ((downloadableContent.Accessibility & DownloadableContentAccessibility.Activated) == DownloadableContentAccessibility.Activated);
			}
		}
		this.DlcContainer.ReserveChildren(this.filteredDownloadableContents.Count, this.DlcDescriptionPrefab, "Item");
		this.DlcContainer.RefreshChildrenIList<DownloadableContent>((from dlc in this.filteredDownloadableContents
		orderby dlc.Number descending
		select dlc).ToList<DownloadableContent>(), new AgeTransform.RefreshTableItem<DownloadableContent>(this.SetupDownloadableContentContainer), true, false);
		this.DlcContainer.Height = this.currentY;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		this.currentY = 0f;
		this.downloadableContentActivationState.Clear();
		IDownloadableContentService downloadableContentService = Services.GetService<IDownloadableContentService>();
		if (downloadableContentService != null)
		{
			this.downloadableContents = (from downloadableContent in downloadableContentService
			where downloadableContent.Type != DownloadableContentType.Undefined
			select downloadableContent).ToList<DownloadableContent>();
			this.downloadableContents.Sort((DownloadableContent left, DownloadableContent right) => left.ReleaseDate.CompareTo(right.ReleaseDate));
		}
		yield break;
	}

	protected override IEnumerator OnHide(bool instant)
	{
		yield return base.OnHide(instant);
		base.GuiService.GetGuiPanel<MenuMainScreen>().RefreshContent();
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.ExpansionsToggle.State = true;
		this.AddOnsToggle.State = false;
		this.UpdatesToggle.State = false;
		this.modified = false;
		this.RefreshContent();
		this.ApplyButton.Enable = false;
		this.DlcScrollView.ResetUp();
		yield break;
	}

	protected override void OnUnload()
	{
		base.OnUnload();
	}

	private void DoCancelRequest(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			base.HandleCancelRequest();
		}
	}

	private void DoClose(object sender, MessagePanelResultEventArgs e)
	{
		if (e.Result == MessagePanelResult.Yes)
		{
			this.Hide(false);
		}
	}

	private void OnApplyCB(GameObject obj)
	{
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null)
		{
			foreach (DownloadableContent downloadableContent in service)
			{
				bool flag;
				if (this.downloadableContentActivationState.TryGetValue(downloadableContent.Name, out flag))
				{
					if (flag)
					{
						downloadableContent.Accessibility |= DownloadableContentAccessibility.Activated;
					}
					else
					{
						downloadableContent.Accessibility &= ~DownloadableContentAccessibility.Activated;
					}
					string x = string.Format("Preferences/DownloadableContents/DownloadableContent{0}/Activated", downloadableContent.Number);
					Amplitude.Unity.Framework.Application.Registry.SetValue<bool>(x, flag);
					base.GuiService.GetGuiPanel<MenuMainScreen>().RefreshContent();
				}
			}
		}
		DepartmentOfScience.ConstructibleElement.ReleaseUnlocksByTechnology();
		this.Hide(false);
	}

	private void OnCloseCB(GameObject obj)
	{
		if (this.modified)
		{
			MessagePanel.Instance.Show("%ConfirmUndoDLCchanges", string.Empty, MessagePanelButtons.YesNo, new MessagePanel.EventHandler(this.DoClose), MessagePanelType.IMPORTANT, new MessagePanelButton[0]);
			return;
		}
		this.Hide(false);
	}

	private void OnFilterExpansionsCB(GameObject obj)
	{
		this.ExpansionsToggle.State = true;
		this.AddOnsToggle.State = false;
		this.UpdatesToggle.State = false;
		this.RefreshContent();
	}

	private void OnFilterAddOnsCB(GameObject obj)
	{
		this.ExpansionsToggle.State = false;
		this.AddOnsToggle.State = true;
		this.UpdatesToggle.State = false;
		this.RefreshContent();
	}

	private void OnFilterUpdatesCB(GameObject obj)
	{
		this.ExpansionsToggle.State = false;
		this.AddOnsToggle.State = false;
		this.UpdatesToggle.State = true;
		this.RefreshContent();
	}

	private void OnStoreCB(GameObject obj)
	{
		AgeControlButton component = obj.GetComponent<AgeControlButton>();
		StaticString name = component.OnActivateData;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		DownloadableContent downloadableContent;
		if (service != null && service.TryGetValue(name, out downloadableContent) && downloadableContent.SteamAppId != 0u && downloadableContent.SteamAppId != 4294967295u)
		{
			Steamworks.SteamFriends steamFriends = Steamworks.SteamAPI.SteamFriends;
			if (steamFriends != null)
			{
				steamFriends.ActivateGameOverlayToStore(downloadableContent.SteamAppId, Steamworks.EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
			}
		}
	}

	private void OnActivateDlcCB(GameObject obj)
	{
		AgeControlToggle component = obj.GetComponent<AgeControlToggle>();
		StaticString staticString = component.OnSwitchData;
		if (!StaticString.IsNullOrEmpty(staticString) && this.downloadableContentActivationState.ContainsKey(staticString))
		{
			this.downloadableContentActivationState[staticString] = component.State;
			this.modified = true;
			this.ApplyButton.Enable = true;
		}
	}

	private void SetupDownloadableContentContainer(AgeTransform tableitem, DownloadableContent downloadableContent, int index)
	{
		DlcDescription component = tableitem.GetComponent<DlcDescription>();
		if (component != null)
		{
			component.SetupContent(downloadableContent, base.gameObject, this.downloadableContentActivationState[downloadableContent.Name]);
			tableitem.Y = this.currentY;
			this.currentY += tableitem.Height + this.DlcContainer.VerticalSpacing;
		}
	}

	public AgePrimitiveLabel PanelTitle;

	public AgeTransform ApplyButton;

	public AgeTransform DlcContainer;

	public AgeControlScrollView DlcScrollView;

	public Transform DlcDescriptionPrefab;

	public AgeControlToggle ExpansionsToggle;

	public AgeControlToggle AddOnsToggle;

	public AgeControlToggle UpdatesToggle;

	private float currentY;

	private bool modified;

	private List<DownloadableContent> downloadableContents = new List<DownloadableContent>();

	private List<DownloadableContent> filteredDownloadableContents = new List<DownloadableContent>();

	private Dictionary<StaticString, bool> downloadableContentActivationState = new Dictionary<StaticString, bool>();
}
