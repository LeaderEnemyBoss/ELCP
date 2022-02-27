using System;
using System.Collections;
using System.Collections.Generic;
using Amplitude;
using Amplitude.Unity.Framework;
using Amplitude.Unity.Game;
using UnityEngine;

public class GameKaijuResearchScreen : GuiPlayerControllerScreen
{
	public GameKaijuResearchScreen()
	{
		base.EnableCaptureBackgroundOnShow = true;
		base.EnableMenuAudioLayer = true;
		base.EnableNavigation = true;
	}

	private IEndTurnService EndTurnService
	{
		get
		{
			return this.endTurnService;
		}
		set
		{
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange -= this.EndTurnService_GameClientStateChange;
			}
			this.endTurnService = value;
			if (this.endTurnService != null)
			{
				this.endTurnService.GameClientStateChange += this.EndTurnService_GameClientStateChange;
			}
		}
	}

	private IKaijuTechsService KaijuTechsService
	{
		get
		{
			return this.kaijuTechsService;
		}
		set
		{
			if (this.kaijuTechsService != null)
			{
				this.kaijuTechsService.KaijuTechnologyUnlocked -= this.OnKaijuTechnologyUnlocked;
				this.kaijuTechsService.ResearchQueueChanged -= this.OnKaijuResearchQueueChanged;
			}
			this.kaijuTechsService = value;
			if (this.kaijuTechsService != null)
			{
				this.kaijuTechsService.KaijuTechnologyUnlocked += this.OnKaijuTechnologyUnlocked;
				this.kaijuTechsService.ResearchQueueChanged += this.OnKaijuResearchQueueChanged;
			}
		}
	}

	private DepartmentOfTheTreasury DepartmentOfTheTreasury
	{
		get
		{
			return this.departmentOfTheTreasury;
		}
		set
		{
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange -= this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
			this.departmentOfTheTreasury = value;
			if (this.departmentOfTheTreasury != null)
			{
				this.departmentOfTheTreasury.ResourcePropertyChange += this.DepartmentOfTheTreasury_ResourcePropertyChange;
			}
		}
	}

	public override void Bind(global::Empire empire)
	{
		if (empire == null)
		{
			throw new ArgumentNullException("empire");
		}
		base.Bind(empire);
		this.Kaijus = new List<Kaiju>();
		this.DepartmentOfTheTreasury = base.Empire.GetAgency<DepartmentOfTheTreasury>();
		this.KaijuUnlocksFrame.SetupFrame(base.Empire, base.gameObject);
		base.NeedRefresh = true;
	}

	protected override IEnumerator OnLoad()
	{
		yield return base.OnLoad();
		base.UseRefreshLoop = true;
		yield break;
	}

	protected override IEnumerator OnLoadGame()
	{
		yield return base.OnLoadGame();
		this.EndTurnService = Services.GetService<IEndTurnService>();
		this.SaveLayoutButton.AgeTransform.Visible = false;
		this.KaijuTechsService = base.GameService.Game.Services.GetService<IKaijuTechsService>();
		Diagnostics.Assert(this.KaijuTechsService != null);
		yield break;
	}

	protected override IEnumerator OnShow(params object[] parameters)
	{
		yield return base.OnShow(parameters);
		this.KaijuUnlocksFrame.SetSimpleMode(false);
		this.ObtainKaijus();
		this.CreateKaijuPortraits();
		this.LayoutGroup.Visible = false;
		base.NeedRefresh = true;
		yield break;
	}

	public override void RefreshContent()
	{
		if (base.Empire == null)
		{
			return;
		}
		this.interactionsAllowed = base.PlayerController.CanSendOrders();
		if (base.IsVisible)
		{
			this.RefreshUnlocksSection();
			this.RefreshBuyoutButton();
		}
	}

	protected override IEnumerator OnHide(bool instant)
	{
		base.NeedRefresh = false;
		return base.OnHide(instant);
	}

	protected override void OnUnloadGame(IGame game)
	{
		this.EndTurnService = null;
		this.KaijuTechsService = null;
		base.OnUnloadGame(game);
	}

	private void ObtainKaijus()
	{
		this.Kaijus.Clear();
		MajorEmpire[] array = Array.ConvertAll<global::Empire, MajorEmpire>(Array.FindAll<global::Empire>((base.Game as global::Game).Empires, (global::Empire match) => match is MajorEmpire), (global::Empire empire) => empire as MajorEmpire);
		KaijuEmpire[] array2 = Array.ConvertAll<global::Empire, KaijuEmpire>(Array.FindAll<global::Empire>((base.Game as global::Game).Empires, (global::Empire match) => match is KaijuEmpire), (global::Empire empire) => empire as KaijuEmpire);
		for (int i = 0; i < array.Length; i++)
		{
			if (array[i].TamedKaijus.Count > 0)
			{
				List<Kaiju> tamedKaijus = array[i].TamedKaijus;
				for (int j = 0; j < tamedKaijus.Count; j++)
				{
					this.Kaijus.Add(tamedKaijus[j]);
				}
			}
		}
		for (int k = 0; k < array2.Length; k++)
		{
			KaijuCouncil agency = array2[k].GetAgency<KaijuCouncil>();
			if (agency.Kaiju != null)
			{
				this.Kaijus.Add(agency.Kaiju);
			}
		}
	}

	private void CreateKaijuPortraits()
	{
		this.PortraitsContainer.ReserveChildren(this.Kaijus.Count, this.KaijuPortraitPrefab, "Item");
		this.PortraitsContainer.RefreshChildrenIList<Kaiju>(this.Kaijus, new AgeTransform.RefreshTableItem<Kaiju>(this.SetupKaijuPortraits), true, false);
	}

	private void SetupKaijuPortraits(AgeTransform tableitem, Kaiju kaiju, int index)
	{
		KaijuTechPortrait component = tableitem.GetComponent<KaijuTechPortrait>();
		component.SetupPortrait(kaiju, base.Empire);
		if (component.KaijuGuiElement != null)
		{
			component.AgeTransform.X = ((!AgeUtils.HighDefinition) ? component.KaijuGuiElement.X : (component.KaijuGuiElement.X * AgeUtils.HighDefinitionFactor));
			component.AgeTransform.Y = ((!AgeUtils.HighDefinition) ? component.KaijuGuiElement.Y : (component.KaijuGuiElement.Y * AgeUtils.HighDefinitionFactor));
			return;
		}
		Diagnostics.LogError("The Kaiju doesn't have a KaijuTechPortrait");
		component.AgeTransform.X = 0f;
		component.AgeTransform.Y = 0f;
	}

	private void RefreshUnlocksSection()
	{
		this.KaijuUnlocksFrame.RefreshFrame();
	}

	private void RefreshBuyoutButton()
	{
		ConstructionQueue constructionQueueForEmpire = this.kaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		ConstructibleElement constructibleElement = null;
		if (constructionQueueForEmpire.Peek() != null)
		{
			constructibleElement = constructionQueueForEmpire.Peek().ConstructibleElement;
		}
		int num = 0;
		string text;
		PanelFeatureCost.ComputeCostAndTurn(base.GuiService, constructionQueueForEmpire.PendingConstructions, this.departmentOfTheTreasury, base.Empire, out text, out num);
		if (constructibleElement != null)
		{
			if (this.CanAffordTechs())
			{
				this.BuyoutCostLabel.Text = text;
				this.BuyoutButton.AgeTransform.Enable = true;
				this.BuyoutButton.AgeTransform.Alpha = 1f;
				string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutAvailableFormat").Replace("$Cost", text);
			}
			else
			{
				this.BuyoutCostLabel.Text = text;
				this.BuyoutButton.AgeTransform.Enable = false;
				this.BuyoutButton.AgeTransform.Alpha = 0.5f;
				string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutUnavailableFormat").Replace("$Cost", text);
			}
		}
		else
		{
			this.BuyoutButton.AgeTransform.Enable = false;
			this.BuyoutButton.AgeTransform.Alpha = 0.5f;
			this.BuyoutCostLabel.Text = "%ResearchVoidSymbol";
			string text2 = AgeLocalizer.Instance.LocalizeString("%KaijuUnlockBuyoutNoSelectionDescription");
		}
	}

	private bool CanAffordTechs()
	{
		ConstructionQueue constructionQueueForEmpire = this.kaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		Dictionary<string, float> dictionary = new Dictionary<string, float>();
		for (int i = 0; i < constructionQueueForEmpire.Length; i++)
		{
			ConstructibleElement constructibleElement = constructionQueueForEmpire.PeekAt(i).ConstructibleElement;
			for (int j = 0; j < constructibleElement.Costs.Length; j++)
			{
				string key = constructibleElement.Costs[j].ResourceName;
				float num = -constructibleElement.Costs[j].GetValue(base.Empire);
				if (!dictionary.ContainsKey(constructibleElement.Costs[j].ResourceName))
				{
					dictionary.Add(constructibleElement.Costs[j].ResourceName, num);
				}
				else
				{
					num += dictionary[key];
					dictionary[key] = num;
				}
			}
		}
		foreach (KeyValuePair<string, float> keyValuePair in dictionary)
		{
			float value = keyValuePair.Value;
			if (!this.DepartmentOfTheTreasury.IsTransferOfResourcePossible(base.Empire, keyValuePair.Key, ref value))
			{
				return false;
			}
		}
		return true;
	}

	private void EndTurnService_GameClientStateChange(object sender, GameClientStateChangeEventArgs e)
	{
		if (base.PlayerController != null)
		{
			bool flag = base.PlayerController.CanSendOrders();
			if (this.interactionsAllowed != flag)
			{
				this.interactionsAllowed = flag;
				if (!flag && MessagePanel.Instance.IsVisible)
				{
					MessagePanel.Instance.Hide(false);
				}
			}
		}
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void DepartmentOfTheTreasury_ResourcePropertyChange(object sender, ResourcePropertyChangeEventArgs e)
	{
		if (e.Location != base.Empire.SimulationObject)
		{
			return;
		}
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void OnKaijuTechnologyUnlocked(object sender, ConstructibleElementEventArgs e)
	{
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void OnKaijuResearchQueueChanged(object sender, ConstructionChangeEventArgs e)
	{
		if (base.IsVisible)
		{
			base.NeedRefresh = true;
		}
	}

	private void OnSelectTechnology(AgeTransform technology)
	{
	}

	private void OnTechnologyBuyoutCB(GameObject obj)
	{
		ConstructionQueue constructionQueueForEmpire = this.KaijuTechsService.GetConstructionQueueForEmpire(base.Empire);
		if (constructionQueueForEmpire != null)
		{
			int length = constructionQueueForEmpire.Length;
			for (int i = length - 1; i >= 0; i--)
			{
				Construction construction = constructionQueueForEmpire.PeekAt(i);
				if (construction == null)
				{
					return;
				}
				OrderBuyOutKaijuTechnology order = new OrderBuyOutKaijuTechnology(base.Empire.Index, construction.ConstructibleElement.Name);
				base.PlayerController.PostOrder(order);
			}
		}
	}

	private void OnCloseCB(GameObject obj)
	{
		this.HandleCancelRequest();
	}

	public override bool HandleCancelRequest()
	{
		bool flag = false;
		if (Input.GetMouseButtonDown(1))
		{
			flag = base.GuiService.GetGuiPanel<NotificationListPanel>().HandleRightClick();
		}
		if (!flag)
		{
			base.GuiService.GetGuiPanel<GameWorldScreen>().Show(new object[0]);
			flag = true;
		}
		return flag;
	}

	private void OnEditLayoutCB(GameObject obj)
	{
		this.SaveLayoutButton.AgeTransform.Visible = this.LayoutToggle.State;
		TechnologyFrame[] componentsInChildren = base.GetComponentsInChildren<TechnologyFrame>();
		for (int i = 0; i < componentsInChildren.Length; i++)
		{
			componentsInChildren[i].ActivateMarkup(this.LayoutToggle.State);
		}
	}

	private void OnSaveLayoutCB(GameObject obj)
	{
	}

	public AgeTransform LayoutGroup;

	public AgeControlToggle LayoutToggle;

	public AgeControlButton SaveLayoutButton;

	public KaijuUnlocksFrame KaijuUnlocksFrame;

	public AgeControlButton BuyoutButton;

	public AgePrimitiveLabel BuyoutCostLabel;

	public AgeTransform PortraitsContainer;

	public Transform KaijuPortraitPrefab;

	private bool interactionsAllowed = true;

	private IEndTurnService endTurnService;

	private IKaijuTechsService kaijuTechsService;

	private DepartmentOfTheTreasury departmentOfTheTreasury;

	private DepartmentOfScience departmentOfScience;

	private List<Kaiju> Kaijus;
}
