using System;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using Amplitude.Unity.Gui;
using UnityEngine;

public class TechnologyFrame : MonoBehaviour
{
	public List<string> TagsList { get; private set; }

	public TechnologyDefinition TechnologyDefinition { get; private set; }

	public TechnologyGuiElement TechnologyGuiElement
	{
		get
		{
			IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
			Diagnostics.Assert(guiPanelHelper != null, "Unable to access GuiPanelHelper");
			GuiElement guiElement;
			if (guiPanelHelper.TryGetGuiElement(this.TechnologyDefinition.Name, out guiElement))
			{
				return guiElement as TechnologyGuiElement;
			}
			return null;
		}
	}

	public void ActivateMarkup(bool markup)
	{
		this.MarkupGroup.Visible = markup;
	}

	public void DockTooltip(bool status)
	{
		if (status)
		{
			this.AgeTransform.AgeTooltip.AnchorMode = AgeTooltipAnchorMode.RIGHT_CENTER;
		}
		else
		{
			this.AgeTransform.AgeTooltip.AnchorMode = AgeTooltipAnchorMode.FREE;
		}
	}

	public void Refresh(global::Empire empire, DepartmentOfScience.ConstructibleElement.State state)
	{
		if (this.Button != null)
		{
			this.Button.AgeTransform.Enable = true;
		}
		TechnologyDefinitionVisibility visibility = this.TechnologyDefinition.Visibility;
		if (visibility != TechnologyDefinitionVisibility.VisibleWhenUnlocked)
		{
			if (visibility == TechnologyDefinitionVisibility.BasedOnPrerequisites)
			{
				bool flag = DepartmentOfTheTreasury.CheckConstructiblePrerequisites(empire, this.TechnologyDefinition, new string[]
				{
					"Visibility"
				});
				if (flag)
				{
					this.AgeTransform.Visible = true;
				}
				else
				{
					this.AgeTransform.Visible = false;
				}
			}
		}
		else
		{
			if (state != DepartmentOfScience.ConstructibleElement.State.Researched)
			{
				this.AgeTransform.Visible = false;
				return;
			}
			if (!this.AgeTransform.Visible)
			{
				this.AgeTransform.Visible = true;
				this.SetSimpleMode(false);
			}
		}
		this.UnlockDisabled.Visible = false;
		this.InProgressSector.AgeTransform.Visible = false;
		this.OrderCaption.AgeTransform.Visible = false;
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		bool flag2 = false;
		int num = 0;
		ConstructionQueue constructionQueueForTech = agency.GetConstructionQueueForTech(this.TechnologyDefinition);
		if ((state == DepartmentOfScience.ConstructibleElement.State.Queued || state == DepartmentOfScience.ConstructibleElement.State.InProgress) && constructionQueueForTech.Length > 1)
		{
			flag2 = true;
			if (state == DepartmentOfScience.ConstructibleElement.State.Queued)
			{
				for (int i = 0; i < constructionQueueForTech.Length; i++)
				{
					DepartmentOfScience.ConstructibleElement constructibleElement = constructionQueueForTech.PeekAt(i).ConstructibleElement as DepartmentOfScience.ConstructibleElement;
					if (constructibleElement.Name == this.TechnologyDefinition.Name)
					{
						num = i;
						break;
					}
				}
			}
		}
		bool flag3 = this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Affinity) || this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Medal) || this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Quest);
		bool flag4 = this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock);
		Color tintColor;
		Color tintColor2;
		Color tintColor3;
		Color tintColor4;
		switch (state)
		{
		case DepartmentOfScience.ConstructibleElement.State.Available:
			tintColor = ((!flag4) ? this.AvailableColor : this.AvailableOrbUnlockColor);
			tintColor2 = ((!flag3) ? ((!flag4) ? this.AvailableBackdropColor : this.AvailableOrbUnlockBackdropColor) : this.AvailableBackdropColorAffinity);
			tintColor3 = this.AvailableSymbolColor;
			tintColor4 = this.GlowAvailableColor;
			break;
		case DepartmentOfScience.ConstructibleElement.State.Queued:
			tintColor = this.QueuedColor;
			tintColor2 = ((!flag3) ? this.QueuedBackdropColor : this.QueuedBackdropColorAffinity);
			tintColor3 = this.QueuedSymbolColor;
			tintColor4 = this.GlowAvailableColor;
			this.OrderCaption.AgeTransform.Visible = true;
			this.OrderCaption.TintColor = this.QueuedColor;
			this.OrderLabel.Text = (num + 1).ToString();
			break;
		case DepartmentOfScience.ConstructibleElement.State.InProgress:
			tintColor = this.InProgressColor;
			tintColor2 = ((!flag3) ? this.InProgressBackdropColor : this.InProgressBackdropColorAffinity);
			tintColor3 = this.InProgressSymbolColor;
			tintColor4 = this.GlowAvailableColor;
			this.InProgressSector.AgeTransform.Visible = true;
			if (flag2)
			{
				this.OrderCaption.AgeTransform.Visible = true;
				this.OrderCaption.TintColor = this.InProgressColor;
				this.OrderLabel.Text = (num + 1).ToString();
			}
			break;
		case DepartmentOfScience.ConstructibleElement.State.Researched:
			if (this.Button != null)
			{
				this.Button.AgeTransform.Enable = false;
			}
			tintColor = ((!flag4) ? this.ResearchedColor : this.ResearchedOrbUnlockColor);
			tintColor2 = ((!flag3) ? this.ResearchedBackdropColor : this.ResearchedBackdropColorAffinity);
			tintColor3 = this.ResearchedSymbolColor;
			tintColor4 = this.GlowAvailableColor;
			break;
		case DepartmentOfScience.ConstructibleElement.State.ResearchedButUnavailable:
			if (this.Button != null)
			{
				this.Button.AgeTransform.Enable = false;
			}
			tintColor = this.ResearchedButUnavailableColor;
			tintColor2 = ((!flag3) ? this.ResearchedBackdropColor : this.ResearchedBackdropColorAffinity);
			tintColor3 = this.ResearchedSymbolColor;
			tintColor4 = this.GlowNotAvailableColor;
			this.UnlockDisabled.Visible = true;
			break;
		default:
			tintColor = this.NotAvailableColor;
			tintColor2 = ((!flag3) ? this.NotAvailableBackdropColor : this.NotAvailableBackdropColorAffinity);
			tintColor3 = this.NotAvailableSymbolColor;
			tintColor4 = this.GlowNotAvailableColor;
			if (this.Button != null)
			{
				this.Button.AgeTransform.Enable = false;
			}
			this.UnlockDisabled.Visible = true;
			break;
		}
		this.CircularFrame.TintColor = tintColor;
		this.CaptionFullBackground.TintColor = tintColor2;
		this.CaptionTopBackground.TintColor = tintColor2;
		this.CaptionBottomBackground.TintColor = tintColor2;
		this.EraLabel.TintColor = tintColor3;
		this.CategoryIcon.TintColor = tintColor3;
		this.SubCategoryIcon.TintColor = tintColor3;
		this.CategoryFullIcon.TintColor = tintColor3;
		this.SubCategoryFullIcon.TintColor = tintColor3;
		this.GlowImage.TintColor = tintColor4;
		this.RefreshCostGroup(state);
	}

	public void SetSimpleMode(bool simple)
	{
		this.UnlockImage.AgeTransform.Visible = !simple;
		this.CaptionTop.Visible = !simple;
		this.CaptionBottom.Visible = !simple;
		this.CaptionFull.Visible = simple;
		this.OrderCaption.AgeTransform.Alpha = (float)((!simple) ? 1 : 0);
	}

	public void SetupTechnology(global::Empire empire, TechnologyDefinition technologyDefinition, GameObject client)
	{
		this.empire = empire;
		this.TechnologyDefinition = technologyDefinition;
		this.selectionClient = client;
		if (this.TagsList == null)
		{
			this.TagsList = new List<string>();
		}
		else
		{
			this.TagsList.Clear();
		}
		if (this.empire == null)
		{
			return;
		}
		this.BuildTags();
		if (technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Quest))
		{
			this.EraLabel.Text = string.Empty;
			AgePrimitiveLabel eraLabel = this.EraLabel;
			eraLabel.Text += (char)this.QuestCharNumber;
		}
		else if (technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Medal))
		{
			this.EraLabel.Text = string.Empty;
			AgePrimitiveLabel eraLabel2 = this.EraLabel;
			eraLabel2.Text += (char)this.MedalCharNumber;
		}
		else if (technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Affinity))
		{
			this.EraLabel.Text = string.Empty;
			AgePrimitiveLabel eraLabel3 = this.EraLabel;
			eraLabel3.Text += GuiEmpire.GetFactionSymbolString(this.empire, this.empire);
		}
		else if (technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.OrbUnlock))
		{
			this.EraLabel.Text = string.Empty;
		}
		else if (technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock))
		{
			this.EraLabel.Text = string.Empty;
		}
		else
		{
			int technologyEraNumber = DepartmentOfScience.GetTechnologyEraNumber(technologyDefinition);
			if (technologyEraNumber > 0)
			{
				this.EraLabel.Text = AgeUtils.ToRoman(technologyEraNumber);
			}
			else
			{
				this.EraLabel.Text = "-";
			}
		}
		this.GlowImage.AgeTransform.Visible = false;
		IDownloadableContentService service = Services.GetService<IDownloadableContentService>();
		if (service != null && service.IsShared(DownloadableContent9.ReadOnlyName) && technologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.Unique))
		{
			this.GlowImage.AgeTransform.Visible = true;
		}
		if (this.UnlockImage != null)
		{
			this.UnlockImage.Image = DepartmentOfScience.GetTechnologyImage(technologyDefinition, global::GuiPanel.IconSize.Small);
		}
		this.CategoryIcon.Image = DepartmentOfScience.GetCategoryIcon(technologyDefinition, global::GuiPanel.IconSize.Small);
		this.CategoryFullIcon.Image = this.CategoryIcon.Image;
		if (this.SubCategoryIcon.Image != null)
		{
			this.CategoryIcon.AgeTransform.PixelOffsetLeft = -this.CategoryIcon.AgeTransform.Width;
			this.CategoryFullIcon.AgeTransform.PixelOffsetLeft = -this.CategoryFullIcon.AgeTransform.Width;
		}
		else
		{
			this.CategoryIcon.AgeTransform.PixelOffsetLeft = -(0.5f * this.CategoryIcon.AgeTransform.Width);
			this.CategoryFullIcon.AgeTransform.PixelOffsetLeft = -(0.5f * this.CategoryFullIcon.AgeTransform.Width);
		}
		this.SubCategoryIcon.Image = DepartmentOfScience.GetSubCategoryIcon(technologyDefinition, global::GuiPanel.IconSize.Small);
		this.SubCategoryFullIcon.Image = this.SubCategoryIcon.Image;
		DepartmentOfScience.BuildTechnologyTooltip(technologyDefinition, this.empire, this.AgeTransform.AgeTooltip, MultipleConstructibleTooltipData.TechnologyState.Normal);
		this.InProgressSector.TintColor = this.InProgressColor;
		this.MarkupGroup.Visible = false;
		DepartmentOfScience agency = empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(technologyDefinition);
		this.Refresh(empire, technologyState);
	}

	private void BuildTags()
	{
		this.TagsList.Clear();
		this.TagsList.AddRange(AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetTechnologyTitle(this.TechnologyDefinition)).ToUpper().Split(new char[]
		{
			' '
		}));
		this.TagsList.Add(AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetCategoryTitle(this.TechnologyDefinition)).ToUpper());
		this.TagsList.Add(AgeLocalizer.Instance.LocalizeString(DepartmentOfScience.GetSubCategoryTitle(this.TechnologyDefinition)).ToUpper());
		List<ConstructibleElement> unlocksByTechnology = this.TechnologyDefinition.GetUnlocksByTechnology();
		if (unlocksByTechnology != null && unlocksByTechnology.Count > 1)
		{
			for (int i = 0; i < unlocksByTechnology.Count; i++)
			{
				IGuiPanelHelper guiPanelHelper = Services.GetService<global::IGuiService>().GuiPanelHelper;
				GuiElement guiElement;
				if (guiPanelHelper.TryGetGuiElement(unlocksByTechnology[i].Name, out guiElement))
				{
					this.TagsList.AddRange(AgeLocalizer.Instance.LocalizeString(guiElement.Title).ToUpper().Split(new char[]
					{
						' '
					}));
				}
			}
		}
	}

	private void OnDestroy()
	{
		if (this.AgeTransform.AgeTooltip != null)
		{
			this.AgeTransform.AgeTooltip.ClientData = null;
		}
	}

	private void OnSelectOrbUnlockCB(GameObject obj)
	{
		if (this.empire == null)
		{
			Diagnostics.LogError("Empire is null");
			return;
		}
		DepartmentOfScience agency = this.empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(this.TechnologyDefinition);
		ConstructionQueue constructionQueueForTech = agency.GetConstructionQueueForTech(this.TechnologyDefinition);
		if (technologyState == DepartmentOfScience.ConstructibleElement.State.Available && agency.OrbUnlockQueue.Length < 2)
		{
			this.AgeTransform.Enable = false;
			if (agency.OrbUnlockQueue.Length > 0)
			{
				Construction construction = constructionQueueForTech.Peek();
				this.CancelResearch(construction);
			}
			this.QueueResearch();
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
		}
		else if (technologyState == DepartmentOfScience.ConstructibleElement.State.InProgress || technologyState == DepartmentOfScience.ConstructibleElement.State.Queued)
		{
			this.AgeTransform.Enable = false;
			Construction construction2 = constructionQueueForTech.Get(this.TechnologyDefinition);
			this.CancelResearch(construction2);
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
		}
	}

	private void OnSelectTechnologyCB(GameObject obj)
	{
		if (this.empire == null)
		{
			Diagnostics.LogError("Empire is null");
			return;
		}
		DepartmentOfScience agency = this.empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(this.TechnologyDefinition);
		ConstructionQueue constructionQueueForTech = agency.GetConstructionQueueForTech(this.TechnologyDefinition);
		if (technologyState == DepartmentOfScience.ConstructibleElement.State.Available)
		{
			this.AgeTransform.Enable = false;
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && Input.GetKey(KeyCode.G))
			{
				this.ForceUnlockTechnology();
			}
			else
			{
				this.QueueResearch();
			}
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
			return;
		}
		if (technologyState == DepartmentOfScience.ConstructibleElement.State.InProgress || technologyState == DepartmentOfScience.ConstructibleElement.State.Queued)
		{
			this.AgeTransform.Enable = false;
			Construction construction = constructionQueueForTech.Get(this.TechnologyDefinition);
			this.CancelResearch(construction);
			if (Amplitude.Unity.Framework.Application.Preferences.EnableModdingTools && Input.GetKey(KeyCode.G))
			{
				this.ForceUnlockTechnology();
			}
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
		}
	}

	private void OnSelectKaijuUnlockCB(GameObject obj)
	{
		if (this.empire == null)
		{
			Diagnostics.LogError("Empire is null");
			return;
		}
		IGameService service = Services.GetService<IGameService>();
		if (service == null || service.Game == null)
		{
			Diagnostics.LogError("GameService or GameService.Game is null");
			return;
		}
		IKaijuTechsService service2 = service.Game.Services.GetService<IKaijuTechsService>();
		DepartmentOfScience.ConstructibleElement.State technologyState = service2.GetTechnologyState(this.TechnologyDefinition, this.empire);
		ConstructionQueue constructionQueueForEmpire = service2.GetConstructionQueueForEmpire(this.empire);
		if (constructionQueueForEmpire == null)
		{
			return;
		}
		if (technologyState == DepartmentOfScience.ConstructibleElement.State.Available)
		{
			this.AgeTransform.Enable = false;
			this.QueueResearch();
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
		}
		else if (technologyState == DepartmentOfScience.ConstructibleElement.State.InProgress || technologyState == DepartmentOfScience.ConstructibleElement.State.Queued)
		{
			this.AgeTransform.Enable = false;
			Construction construction = constructionQueueForEmpire.Get(this.TechnologyDefinition);
			this.CancelResearch(construction);
			this.selectionClient.SendMessage("OnSelectTechnology", this.AgeTransform, SendMessageOptions.RequireReceiver);
		}
	}

	private void CancelResearch(Construction construction)
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (service2 != null && construction != null)
		{
			if (this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock))
			{
				OrderCancelKaijuResearch order = new OrderCancelKaijuResearch(this.empire.Index, construction.GUID);
				Ticket ticket;
				service2.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
			else
			{
				OrderCancelResearch order2 = new OrderCancelResearch(this.empire.Index, construction.GUID);
				Ticket ticket;
				service2.ActivePlayerController.PostOrder(order2, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
			}
		}
	}

	private void QueueResearch()
	{
		IGameService service = Services.GetService<IGameService>();
		Diagnostics.Assert(service != null);
		IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
		if (service2 != null)
		{
			if (this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock))
			{
				OrderQueueKaijuResearch order = new OrderQueueKaijuResearch(this.empire.Index, this.TechnologyDefinition);
				Ticket ticket;
				service2.ActivePlayerController.PostOrder(order, out ticket, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
				return;
			}
			OrderQueueResearch orderQueueResearch = new OrderQueueResearch(this.empire.Index, this.TechnologyDefinition);
			if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			{
				orderQueueResearch.InsertAtFirstPlace = true;
			}
			Ticket ticket2;
			service2.ActivePlayerController.PostOrder(orderQueueResearch, out ticket2, new EventHandler<TicketRaisedEventArgs>(this.OnOrderResponse));
		}
	}

	private void RefreshCostGroup(DepartmentOfScience.ConstructibleElement.State state)
	{
		bool flag = state != DepartmentOfScience.ConstructibleElement.State.Researched && state != DepartmentOfScience.ConstructibleElement.State.ResearchedButUnavailable;
		if (this.ShowCaptionCost && flag)
		{
			Amplitude.Unity.Gui.IGuiService service = Services.GetService<global::IGuiService>();
			Diagnostics.Assert(service != null);
			DepartmentOfTheTreasury agency = this.empire.GetAgency<DepartmentOfTheTreasury>();
			Diagnostics.Assert(agency != null);
			string empty = string.Empty;
			int num = 0;
			PanelFeatureCost.ComputeCostAndTurn(service, this.TechnologyDefinition, agency, this.empire, out empty, out num);
			if (string.IsNullOrEmpty(empty))
			{
				this.CaptionCostGroup.Visible = false;
			}
			else
			{
				this.CostLabel.Text = empty;
				this.CaptionCostGroup.Visible = true;
			}
		}
		else if (this.CaptionCostGroup != null)
		{
			this.CaptionCostGroup.Visible = false;
		}
	}

	private void OnOrderResponse(object sender, TicketRaisedEventArgs args)
	{
		this.AgeTransform.Enable = true;
		DepartmentOfScience agency = this.empire.GetAgency<DepartmentOfScience>();
		DepartmentOfScience.ConstructibleElement.State technologyState = agency.GetTechnologyState(this.TechnologyDefinition);
		if (this.TechnologyDefinition.HasTechnologyFlag(DepartmentOfScience.ConstructibleElement.TechnologyFlag.KaijuUnlock))
		{
			IGameService service = Services.GetService<IGameService>();
			if (service == null || service.Game == null)
			{
				Diagnostics.LogError("GameService or GameService.Game is null");
				return;
			}
			IKaijuTechsService service2 = service.Game.Services.GetService<IKaijuTechsService>();
			technologyState = service2.GetTechnologyState(this.TechnologyDefinition, this.empire);
		}
		this.Refresh(this.empire, technologyState);
	}

	private void ForceUnlockTechnology()
	{
		IGameService service = Services.GetService<IGameService>();
		if (service != null && service.Game != null)
		{
			IPlayerControllerRepositoryService service2 = service.Game.Services.GetService<IPlayerControllerRepositoryService>();
			if (service2 != null && service2.ActivePlayerController != null)
			{
				OrderForceUnlockTechnology order = new OrderForceUnlockTechnology(service2.ActivePlayerController.Empire.Index, this.TechnologyDefinition.Name);
				service2.ActivePlayerController.PostOrder(order);
			}
		}
	}

	public AgeTransform AgeTransform;

	public AgeControlButton Button;

	public AgePrimitiveImage CircularFrame;

	public AgeTransform CaptionCostGroup;

	public AgeTransform CaptionTop;

	public AgeTransform CaptionBottom;

	public AgePrimitiveImage CaptionTopBackground;

	public AgePrimitiveImage CaptionBottomBackground;

	public AgeTransform CaptionFull;

	public AgePrimitiveImage CaptionFullBackground;

	public AgePrimitiveLabel CostLabel;

	public AgePrimitiveLabel EraLabel;

	public AgePrimitiveImage CategoryIcon;

	public AgePrimitiveImage SubCategoryIcon;

	public AgePrimitiveImage CategoryFullIcon;

	public AgePrimitiveImage SubCategoryFullIcon;

	public AgePrimitiveImage UnlockImage;

	public AgeTransform UnlockDisabled;

	public AgePrimitiveSector InProgressSector;

	public AgePrimitiveImage OrderCaption;

	public AgePrimitiveLabel OrderLabel;

	public AgeTransform MarkupGroup;

	public AgePrimitiveImage GlowImage;

	public Color AvailableColor;

	public Color QueuedColor;

	public Color InProgressColor;

	public Color ResearchedColor;

	public Color NotAvailableColor;

	public Color ResearchedButUnavailableColor;

	public Color AvailableBackdropColor;

	public Color QueuedBackdropColor;

	public Color InProgressBackdropColor;

	public Color ResearchedBackdropColor;

	public Color NotAvailableBackdropColor;

	public Color AvailableBackdropColorAffinity;

	public Color QueuedBackdropColorAffinity;

	public Color InProgressBackdropColorAffinity;

	public Color ResearchedBackdropColorAffinity;

	public Color NotAvailableBackdropColorAffinity;

	public Color AvailableSymbolColor;

	public Color QueuedSymbolColor;

	public Color InProgressSymbolColor;

	public Color ResearchedSymbolColor;

	public Color NotAvailableSymbolColor;

	public Color GlowAvailableColor;

	public Color GlowNotAvailableColor;

	public Color AvailableOrbUnlockColor;

	public Color AvailableOrbUnlockBackdropColor;

	public Color ResearchedOrbUnlockColor;

	public int QuestCharNumber;

	public int MedalCharNumber;

	public bool ShowCaptionCost;

	private global::Empire empire;

	private GameObject selectionClient;
}
